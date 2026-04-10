using System;
using System.Collections.Generic;
using Sindy.Reactive;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ReactiveDictionary — Add, Remove, indexer(get/set), TryGetValue, events, 예외
    /// </summary>
    class TestReactiveDictionary : TestCase
    {
        public override void Run()
        {
            AddBasic();
            AddDuplicateThrows();
            RemoveExisting();
            RemoveNonExistingThrows();
            IndexerGet();
            IndexerGetMissingThrows();
            IndexerSetAddsNew();
            IndexerSetUpdatesExisting();
            TryGetValueFound();
            TryGetValueNotFound();
            TryGetValueTyped();
            ContainsKeyCheck();
            CountTracked();
            KeysAndValues();
            OnAddedEventFires();
            OnRemovedEventFires();
            OnUpdatedEventFires();
            EnumerationWorks();
        }

        private void AddBasic()
        {
            var dict = new ReactiveDictionary<string, int>();

            dict.Add("hp", 100);
            dict.Add("mp", 50);

            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(100, dict["hp"]);
            Assert.AreEqual(50, dict["mp"]);
        }

        private void AddDuplicateThrows()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            bool threw = false;
            try
            {
                dict.Add("hp", 200);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
            Assert.AreEqual(100, dict["hp"]);
        }

        private void RemoveExisting()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            dict.Remove("hp");

            Assert.AreEqual(0, dict.Count);
            Assert.IsFalse(dict.ContainsKey("hp"));
        }

        private void RemoveNonExistingThrows()
        {
            var dict = new ReactiveDictionary<string, int>();

            bool threw = false;
            try
            {
                dict.Remove("missing");
            }
            catch (KeyNotFoundException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
        }

        private void IndexerGet()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            Assert.AreEqual(100, dict["hp"]);
        }

        private void IndexerGetMissingThrows()
        {
            var dict = new ReactiveDictionary<string, int>();

            bool threw = false;
            try
            {
                var _ = dict["missing"];
            }
            catch (KeyNotFoundException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
        }

        private void IndexerSetAddsNew()
        {
            var dict = new ReactiveDictionary<string, int>();

            var added = new List<string>();
            dict.OnAdded += (key, _) => added.Add(key);

            dict["hp"] = 100;

            Assert.AreEqual(100, dict["hp"]);
            Assert.AreEqual(1, added.Count);
            Assert.AreEqual("hp", added[0]);
        }

        private void IndexerSetUpdatesExisting()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            int capturedOld = 0;
            int capturedNew = 0;
            dict.OnUpdated += (_, oldVal, newVal) =>
            {
                capturedOld = oldVal;
                capturedNew = newVal;
            };

            dict["hp"] = 200;

            Assert.AreEqual(200, dict["hp"]);
            Assert.AreEqual(100, capturedOld);
            Assert.AreEqual(200, capturedNew);
        }

        private void TryGetValueFound()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            var found = dict.TryGetValue("hp", out var value);

            Assert.IsTrue(found);
            Assert.AreEqual(100, value);
        }

        private void TryGetValueNotFound()
        {
            var dict = new ReactiveDictionary<string, int>();

            var found = dict.TryGetValue("missing", out var value);

            Assert.IsFalse(found);
            Assert.AreEqual(0, value);
        }

        private void TryGetValueTyped()
        {
            var dict = new ReactiveDictionary<string, object>();
            dict.Add("name", "hero");
            dict.Add("level", 10);

            var found = dict.TryGetValue<string>("name", out var name);
            Assert.IsTrue(found);
            Assert.AreEqual("hero", name);

            var wrongType = dict.TryGetValue<string>("level", out var str);
            Assert.IsFalse(wrongType);
        }

        private void ContainsKeyCheck()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            Assert.IsTrue(dict.ContainsKey("hp"));
            Assert.IsFalse(dict.ContainsKey("mp"));
        }

        private void CountTracked()
        {
            var dict = new ReactiveDictionary<string, int>();
            Assert.AreEqual(0, dict.Count);

            dict.Add("a", 1);
            dict.Add("b", 2);
            Assert.AreEqual(2, dict.Count);

            dict.Remove("a");
            Assert.AreEqual(1, dict.Count);
        }

        private void KeysAndValues()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);
            dict.Add("mp", 50);

            var keys = new List<string>(dict.Keys);
            var values = new List<int>(dict.Values);

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(2, values.Count);
            Assert.IsTrue(keys.Contains("hp"));
            Assert.IsTrue(keys.Contains("mp"));
            Assert.IsTrue(values.Contains(100));
            Assert.IsTrue(values.Contains(50));
        }

        private void OnAddedEventFires()
        {
            var dict = new ReactiveDictionary<string, int>();
            string capturedKey = null;
            int capturedValue = 0;
            dict.OnAdded += (key, value) =>
            {
                capturedKey = key;
                capturedValue = value;
            };

            dict.Add("hp", 100);

            Assert.AreEqual("hp", capturedKey);
            Assert.AreEqual(100, capturedValue);
        }

        private void OnRemovedEventFires()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            string capturedKey = null;
            int capturedValue = 0;
            dict.OnRemoved += (key, value) =>
            {
                capturedKey = key;
                capturedValue = value;
            };

            dict.Remove("hp");

            Assert.AreEqual("hp", capturedKey);
            Assert.AreEqual(100, capturedValue);
        }

        private void OnUpdatedEventFires()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            string capturedKey = null;
            int capturedOld = 0;
            int capturedNew = 0;
            dict.OnUpdated += (key, oldVal, newVal) =>
            {
                capturedKey = key;
                capturedOld = oldVal;
                capturedNew = newVal;
            };

            dict["hp"] = 200;

            Assert.AreEqual("hp", capturedKey);
            Assert.AreEqual(100, capturedOld);
            Assert.AreEqual(200, capturedNew);
        }

        private void EnumerationWorks()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            int sum = 0;
            foreach (var kv in dict)
            {
                sum += kv.Value;
            }

            Assert.AreEqual(3, sum);
        }
    }
}
