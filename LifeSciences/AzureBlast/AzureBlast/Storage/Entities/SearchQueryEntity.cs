using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Storage.Entities
{
    public class SearchQueryEntity : TableEntity
    {
        public SearchQueryEntity(Guid searchId, string queryId)
        {
            PartitionKey = searchId.ToString();
            RowKey = queryId;
            CreationTime = DateTime.UtcNow;
        }

        public SearchQueryEntity()
        { }

        public string Id { get { return RowKey; } set { RowKey = value; } }

        public string OutputContainer { get; set; }

        public string QueryFilename { get; set; }

        public string LogOutputFilename { get; set; }

        public string QueryOutputFilename { get; set; }

        public string _State { get; set; }

        [IgnoreProperty]
        public QueryState State
        {
            get { return (QueryState)Enum.Parse(typeof(QueryState), _State); }
            set { _State = value.ToString(); }
        }

        public DateTime CreationTime { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [IgnoreProperty]
        public string Duration
        {
            get
            {
                if (StartTime == null || EndTime == null)
                {
                    return "";
                }
                return (EndTime.Value - StartTime.Value).GetFriendlyDuration();
            }
        }

        [IgnoreProperty]
        public IEnumerable<QueryOutput> Outputs { get; set; }
    }
}
