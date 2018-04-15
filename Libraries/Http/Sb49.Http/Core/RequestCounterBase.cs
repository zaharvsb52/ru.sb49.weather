using System;

namespace Sb49.Http.Core
{
    public abstract class RequestCounterBase
    {
        protected RequestCounterBase(int providerId)
        {
            ProviderId = providerId;
        }

        public int ProviderId { get; }
        public virtual int Count { get; set; }

        /// <summary>
        /// The Utc last update date.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        public abstract void Save();

        public static RequestCounterBase operator ++(RequestCounterBase obj)
        {
            if (obj == null)
                return null;

            obj.Count++;
            return obj;
        }
    }
}