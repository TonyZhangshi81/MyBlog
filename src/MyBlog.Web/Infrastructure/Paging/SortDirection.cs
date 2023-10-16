using System.Runtime.Serialization;

namespace MyBlog.Web.Infrastructure.Paging;

[DataContract]
public enum SortDirection
{
    [EnumMember]
    Ascending,

    [EnumMember]
    Descending
}