import os
import sys
import datetime
import time
import azure.batch.batch_service_client as batch
import azure.batch.batch_auth as batchauth
import azure.batch.models as batchmodels
from azure.storage.table import TableService
from azure.storage.blob import BlockBlobService


def get_state(table_name, all_tasks_complete, any_failures):
    if table_name == 'SearchEntity':
        if all_tasks_complete and any_failures:
            return 'Error'
        if all_tasks_complete:
            return 'Complete'
        return 'Running'

    if table_name == 'DatabaseEntity':
        if all_tasks_complete and any_failures:
            return 'ImportingFailed'
        if all_tasks_complete:
            return 'Ready'
        return 'ImportingRunning'

    raise Exception('Unexpected table name {}'.format(table_name))


def wait_for_tasks_to_complete(
        table_service, batch_client, table_name, entity, job_id):
    """
    Returns when all tasks in the specified job reach the Completed state.
    """

    while True:
        entity = table_service.get_entity(
            table_name, entity.PartitionKey, entity.RowKey)

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

        all_tasks_complete = not incomplete_tasks
        any_failures = len(failed_tasks) > 0
        state = get_state(table_name, all_tasks_complete, any_failures)

        entity.CompletedTasks = len(complete_tasks)
        entity._State = state

        if not incomplete_tasks:
            entity.EndTime = datetime.datetime.utcnow()
            table_service.update_entity(table_name, entity)
            return
        else:
            table_service.update_entity(table_name, entity)
            time.sleep(5)


if __name__ == '__main__':
    storage_account = sys.argv[1]
    storage_key = sys.argv[2]
    batch_account = sys.argv[3]
    batch_key = sys.argv[4]
    batch_url = sys.argv[5]
    table_name = sys.argv[6]
    job_id = sys.argv[7]
    entity_pk = sys.argv[8]
    entity_rk = sys.argv[9]

    table_service = TableService(account_name=storage_account,
                                 account_key=storage_key)
    blob_service = BlockBlobService(account_name=storage_account,
                                 account_key=storage_key)
    credentials = batchauth.SharedKeyCredentials(batch_account, batch_key)
    batch_client = batch.BatchServiceClient(credentials, base_url=batch_url)
    entity = table_service.get_entity(table_name, entity_pk, entity_rk)

    wait_for_tasks_to_complete(
        table_service, batch_client, table_name, entity, job_id)

    if table_name == 'DatabaseEntity':
        container_name = sys.argv[10]
        files = 0
        total_size = 0
        db_type = 'Nucleotide'
        generator = blob_service.list_blobs(
            container_name, prefix=entity_rk + '.')
        for blob in generator:
            files += 1
            total_size += blob.properties.content_length
            extension = blob.name.split(".")[-1]
            if extension.startswith('p'):
                db_type = 'Protein'

        entity = table_service.get_entity(table_name, entity_pk, entity_rk)
        entity.FileCount = files
        entity.TotalSize = total_size
        entity._Type = db_type
        table_service.update_entity(table_name, entity)
