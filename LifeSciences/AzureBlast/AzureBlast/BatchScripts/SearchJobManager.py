import os
import sys
import datetime
import time
import azure.batch.batch_service_client as batch
import azure.batch.batch_auth as batchauth
import azure.batch.models as batchmodels
from azure.storage.table import TableService, TableBatch
from azure.storage.blob import BlockBlobService


def get_search_state(all_tasks_complete, any_failures):
    if all_tasks_complete and any_failures:
        return 'Error'
    if all_tasks_complete:
        return 'Complete'
    return 'Running'


def get_query_state(task):
    if task.state == batchmodels.TaskState.active:
        return 'Waiting'
    if task.state == batchmodels.TaskState.preparing:
        return 'Waiting'
    if task.state == batchmodels.TaskState.running:
        return 'Running'
    if task.state == batchmodels.TaskState.completed:
        if task.execution_info.exit_code == 0:
            return 'Success'
        return 'Error'

def wait_for_tasks_to_complete(
        table_service, batch_client, entity_pk, entity_rk, job_id):
    """
    Returns when all tasks in the specified job reach the Completed state.
    """

    while True:
        entity = table_service.get_entity(
            'SearchEntity', entity_pk, entity_rk)

        tasks = batch_client.task.list(job_id)

        incomplete_tasks = [task for task in tasks if
                            task.id != 'JobManager' and
                            task.state != batchmodels.TaskState.completed]
        complete_tasks = [task for task in tasks if
                            task.id != 'JobManager' and
                            task.state == batchmodels.TaskState.completed]
        failed_tasks = [task for task in complete_tasks if
                            task.execution_info.exit_code != 0 or
                            task.execution_info.scheduling_error is not None]

        queries = table_service.query_entities(
            'SearchQueryEntity',
            filter="PartitionKey eq '{}'".format(entity.RowKey))

        current_batch_count = 0
        updateBatch = TableBatch()

        for task in tasks:
            matching_queries = [q for q in queries if q.RowKey == task.id]
            if not matching_queries:
                print('Could not find query {}'.format(task.id))
                continue
            query = matching_queries[0]
            update = False
            state = get_query_state(task)
            if query._State != state:
                query._State = state
                update = True

            if task.state == batchmodels.TaskState.running:
                if not hasattr(query, 'StartTime'):
                    query.StartTime = task.execution_info.start_time
                    update = True

            if task.state == batchmodels.TaskState.completed:
                if not hasattr(query, 'EndTime'):
                    query.EndTime = task.execution_info.end_time
                    update = True

            if update:
                updateBatch.update_entity(query)
                current_batch_count += 1

            if current_batch_count == 99:
                table_service.commit_batch('SearchQueryEntity', updateBatch)
                current_batch_count = 0
                updateBatch = TableBatch()

        if current_batch_count > 0:
            table_service.commit_batch('SearchQueryEntity', updateBatch)

        all_tasks_complete = not incomplete_tasks
        any_failures = len(failed_tasks) > 0

        entity.CompletedTasks = len(complete_tasks)
        entity._State = get_search_state(all_tasks_complete, any_failures)

        if not incomplete_tasks:
            entity.EndTime = datetime.datetime.utcnow()
            table_service.update_entity('SearchEntity', entity)
            return
        else:
            table_service.update_entity('SearchEntity', entity)
            time.sleep(5)


if __name__ == '__main__':
    storage_account = sys.argv[1]
    storage_key = sys.argv[2]
    batch_account = sys.argv[3]
    batch_key = sys.argv[4]
    batch_url = sys.argv[5]
    job_id = sys.argv[6]
    entity_pk = sys.argv[7]
    entity_rk = sys.argv[8]

    table_service = TableService(account_name=storage_account,
                                 account_key=storage_key)
    blob_service = BlockBlobService(account_name=storage_account,
                                 account_key=storage_key)
    credentials = batchauth.SharedKeyCredentials(batch_account, batch_key)
    batch_client = batch.BatchServiceClient(credentials, base_url=batch_url)
    wait_for_tasks_to_complete(table_service, batch_client, entity_pk, entity_rk, job_id)

