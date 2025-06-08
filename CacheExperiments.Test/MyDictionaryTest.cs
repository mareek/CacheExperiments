using NFluent;

namespace CacheExperiments.Test;

public class MyDictionaryTest
{
    [Fact]
    public void TestBasicFunction()
    {
        var myDic = CreateTestDic([3, 5, 13, 23, 33]);

        Check.That(myDic[3]).IsEqualTo(3);
        Check.That(myDic[5]).IsEqualTo(5);
        Check.That(myDic[13]).IsEqualTo(13);
        Check.That(myDic[23]).IsEqualTo(23);
        Check.That(myDic[33]).IsEqualTo(33);
    }

    [Fact]
    public void TestRemove()
    {
        int[] keyValues = [3, 5, 13, 23, 33];

        foreach (var key in keyValues)
        {
            var testDic = CreateTestDic(keyValues);
            Check.That(testDic.Remove(key)).IsTrue();
            Check.That(testDic.ContainsKey(key)).IsFalse();
            foreach (var otherKey in keyValues.Where(k => k != key))
                Check.That(testDic.ContainsKey(otherKey)).IsTrue();

            testDic[key] = key;
            foreach (var otherKey in keyValues)
                Check.That(testDic.ContainsKey(otherKey)).IsTrue();

            Check.That(testDic.Remove(key)).IsTrue();
            Check.That(testDic.Remove(key)).IsFalse();
        }

        var testDic2 = CreateTestDic(keyValues);
        for (int i = 0; i < keyValues.Length; i++)
        {
            var key = keyValues[i];
            Check.That(testDic2.ContainsKey(key)).IsTrue();
            Check.That(testDic2.Remove(key)).IsTrue();
            Check.That(testDic2.ContainsKey(key)).IsFalse();
        }

        testDic2 = CreateTestDic(keyValues);
        for (int i = keyValues.Length - 1; i >= 0; i--)
        {
            var key = keyValues[i];
            Check.That(testDic2.ContainsKey(key)).IsTrue();
            Check.That(testDic2.Remove(key)).IsTrue();
            Check.That(testDic2.ContainsKey(key)).IsFalse();
        }
    }

    private static MyDictionary<int, int> CreateTestDic(int[] keyValues)
    {
        MyDictionary<int, int> result = new(10);

        foreach (int value in keyValues)
            result[value] = value;

        return result;
    }
}
