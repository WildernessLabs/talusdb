using System;
using System.Threading.Tasks;

namespace WildernessLabs.TalusDB
{
    public interface IPublisher
    {
        Task<bool> PublishItem(object item);
        TimeSpan PublicationPeriod { get; set; }
    }
}
