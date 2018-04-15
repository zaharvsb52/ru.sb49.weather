using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Sb49.Security;

namespace Sb49.Weather.Test.Droid
{
    [TestFixture]
    public class TestsSample
    {
        [SetUp]
        public void Setup() { }


        [TearDown]
        public void Tear() { }

        [Test]
        public void DoTest()
        {
            var secure = new Sb49SecureString("Привет!");
            var json = JsonConvert.SerializeObject(secure);
            var secure2 = JsonConvert.DeserializeObject<Sb49SecureString>(json);
            var result = secure2.Decrypt();
            result.Should().NotBeNull();

            var result2 = new ConcurrentDictionary<int, Sb49SecureString>
            {
                [0] = new Sb49SecureString("0001"),
                [1] = new Sb49SecureString("0002"),
                [2] = new Sb49SecureString("0003")
            };

            var result2json = JsonConvert.SerializeObject(result2, Formatting.Indented);
            var result3 = JsonConvert.DeserializeObject<ConcurrentDictionary<int, Sb49SecureString>>(result2json, new MyConverter());

            //var result3 = new ConcurrentDictionary<int, MyClass>
            //{
            //    [0] = new MyClass {Name = "0001"},
            //    [2] = new MyClass { Name = "0002" },
            //    [3] = new MyClass { Name = "0003" },
            //};

            //var result3json = JsonConvert.SerializeObject(result3, Formatting.Indented);
            //var result4 = JsonConvert.DeserializeObject<ConcurrentDictionary<int, MyClass>>(result3json);
        }

        [Test]
        public void Pass()
        {
            Console.WriteLine("test1");
            Assert.True(true);
        }

        [Test]
        public void Fail()
        {
            Assert.False(true);
        }

        [Test]
        [Ignore("another time")]
        public void Ignore()
        {
            Assert.True(false);
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive("Inconclusive");
        }
    }

    public class MyClass
    {
        public string Name { get; set; }
    }

    public class MyConverter : CustomCreationConverter<IDictionary<int, Sb49SecureString>>
    {
        public override IDictionary<int, Sb49SecureString> Create(Type objectType)
        {
            return new ConcurrentDictionary<int, Sb49SecureString>();
        }
    }

}