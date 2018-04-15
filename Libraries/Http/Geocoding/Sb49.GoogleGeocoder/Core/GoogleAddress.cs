using System;
using Sb49.Geocoder.Core;

namespace Sb49.GoogleGeocoder.Core
{
    public class GoogleAddress : AddressBase
    {
        public GoogleAddress(string provider) : base(provider)
        {
        }

        public override string Address { get { throw new NotImplementedException(); } }
        public override string GetAddress(string delimeter, bool useGetLocality = true)
        {
            throw new NotImplementedException();
        }

        public override bool IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
