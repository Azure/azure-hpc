import sys
from azure.storage.table import TableService


if __name__ == '__main__':
    storage_account = sys.argv[1]
    storage_key = sys.argv[2]
    entity_pk = sys.argv[3]
    entity_rk = sys.argv[4]
    state = sys.argv[5]
    error = None
    if len(sys.argv) == 7:
        error = sys.argv[6]

    table_service = TableService(account_name=storage_account,
                                 account_key=storage_key)

    entity = table_service.get_entity('SearchEntity', entity_pk, entity_rk)

    try:
        if entity._State == 'WaitingForResources':
            entity._State = state
            if error:
                entity.Errors = error
            table_service.update_entity('SearchEntity', entity, if_match=entity.etag)
    except Exception as e:
        print('Error updating entityt {}'.format(e))
