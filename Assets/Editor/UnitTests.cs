
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

class UnitTests : MonoBehaviour
{
    [MenuItem("Unit Tests/Run All Tests")]
    public static void RunAllTests()
    {
        Debug.Log("Running All Unit Tests...");

        // List v List
        {
            var testSize = 1000;
            var test = new List<int>();
            for (int i = 0; i < testSize; i++)
            {
                test.Add(i);
            }
            var list = new List<int>();
            for (int i = 0; i < test.Count; i++)
            {
                list.Add(test[i]);
            }
            int shuffleVal = list[0];
            list.RemoveAt(0);
            for (int i = 0; i < test.Count; i++)
            {
                var index = Random.Range(0, list.Count);
                var newShuffleVal = list[index];
                list.RemoveAt(index);
                list.Add(shuffleVal);
                shuffleVal = newShuffleVal;
            }
            list.Add(shuffleVal);
            int sum = 0;
            for (int i = 0; i < test.Count; i++)
            {
                sum += test[i];
                sum -= list[i];
            }
            Assert.AreEqual(sum, 0);
        }

        // List v FastList
        {
            var testSize = 1000;
            var test = new List<int>();
            for (int i = 0; i < testSize; i++)
            {
                test.Add(i);
            }
            var list = new FastList<int>();
            for (int i = 0; i < test.Count; i++)
            {
                list.Add(test[i]);
            }
            int shuffleVal = list.RemoveAt(0);
            for (int i = 0; i < test.Count; i++)
            {
                var index = Random.Range(0, test.Count);
                if (!list.Filled[index]) continue;
                var newShuffleVal = list.RemoveAt(index);
                list.Add(shuffleVal);
                shuffleVal = newShuffleVal;
            }
            list.Add(shuffleVal);
            int sum = 0;
            for (int i = 0; i < test.Count; i++)
            {
                sum += test[i];
                if (list.Filled[i]) sum -= list.Items[i];
            }
            Assert.AreEqual(sum, 0);
        }

        // MegaVolume
        {
            var megaVol = new MegaVolume();
            var allocations = new List<MegaVolume.Allocation>();
            allocations.Add(megaVol.Allocate(new VecUS(0, 0, 0)));
            try
            {
                megaVol.Allocate(new VecUS(0, 0, 0));
                Debug.LogError("Should have thrown exception");
            }
            catch (System.Exception e)
            {
                Assert.IsTrue(e is MegaVolume.VolumeFullException);
            }
            megaVol.Return(allocations[0]);
            allocations.Clear();
            for (int i = 0; i < 4; i++)
            {
                allocations.Add(megaVol.Allocate(new VecUS(1, 2, 0)));
            }
            for (int i = 0; i < 4; i++)
            {
                allocations.Add(megaVol.Allocate(new VecUS(3, 0, 1)));
            }
            for (int i = 0; i < 16; i++)
            {
                allocations.Add(megaVol.Allocate(new VecUS(2, 2, 2)));
            }
            try
            {
                megaVol.Allocate(new VecUS(4, 4, 4));
                Debug.LogError("Should have thrown exception");
            }
            catch (System.Exception e)
            {
                Assert.IsTrue(e is MegaVolume.VolumeFullException);
            }
            for (int i = 0; i < allocations.Count; i++)
            {
                megaVol.Return(allocations[i]);
            }
            allocations.Clear();
            allocations.Add(megaVol.Allocate(new VecUS(0, 0, 0)));
        }

        Debug.Log("Done!");
    }

    public void FastListTest()
    {
    }
}
#endif
