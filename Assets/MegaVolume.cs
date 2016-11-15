using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public struct VecUS
{
    public ushort x, y, z;

    public VecUS(ushort x, ushort y, ushort z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public override string ToString()
    {
        return "("+x+","+y+","+z+")";
    }
}

/// <summary>
/// Bare bones unsecure list for internal system use, optimized for speed yet expandability
/// </summary>
public class FastList<T>
{
    const int START_SIZE = 2;

    public T[] Items = new T[START_SIZE];
    public bool[] Filled = new bool[START_SIZE];
    public int Last = -1;
    public int Count = 0;

    private Stack<int> _emptySpaces = new Stack<int>();

    public void Add(T item)
    {
        int index;
        if (_emptySpaces.Count > 0)
        {
            index = _emptySpaces.Pop();
        }
        else
        {
            Last++;
            index = Last;
            while (Items.Length <= Last)
            {
                var newSize = Items.Length * 2;
                Array.Resize(ref Items, newSize);
                Array.Resize(ref Filled, newSize);
            }
        }
        Items[index] = item;
        Filled[index] = true;
        Count++;
    }

    public T RemoveAt(int index)
    {
        T removedElement = Items[index];
        Items[index] = default(T);
        Filled[index] = false;
        if (index == Last)
        {
            do
            {
                Last--;
            }
            while (Last >= 0 && !Filled[Last]);
        }
        else _emptySpaces.Push(index);
        Count--;
        return removedElement;
    }

    public override string ToString()
    {
        var o = "FastList { ";
        for (int i = 0; i <= Last; i++)
        {
            if (!Filled[i]) o += "_";
            else o += Items[i];
            if (i != Last) o += ", ";
        }
        o += " }";
        return o;
    }
}

/// <summary>
/// Giant 3D texture that keeps track of the data for all volumes, similar to MegaTexturing technology
/// Allocations are 2^x sizes, can have unique x, y and z resolutions
/// Provides a reference to the UV bounds when allocating from it
/// Allocations recycled when returned to unused list
/// </summary>
public class MegaVolume
{
    /// <summary>
    /// Represents portion of MegaVolume allocated out for use outside the system
    /// </summary>
    public struct Allocation
    {
        /// <summary>
        /// 1 / 2 ^ split is the width of the allocation relative to the mv width
        /// </summary>
        public VecUS Split;
        /// <summary>
        /// integer offset of the allocation at split level
        /// </summary>
        public VecUS Offset;

        public Allocation Clone()
        {
            return new Allocation() { Split = Split, Offset = Offset };
        }

        public override string ToString()
        {
            return "Allocation {Split: " + Split + ", Offset: " + Offset + "}";
        }
    }

    public class VolumeFullException : Exception
    {
        private MegaVolume _volume;
        private VecUS _split;
        public VolumeFullException(MegaVolume volume, VecUS split)
        {
            _volume = volume;
            _split = split;
        }
        public override string Message
        {
            get
            {
                return "Mega Volume couldn't find room for split of " + _split;
            }
        }
    }

        

    private FastList<Allocation> _unused = new FastList<Allocation>();

	public MegaVolume()
    {
        _unused.Add(new Allocation());
	}

    public Allocation Allocate(VecUS split)
    {
        #region Search for closest allocation that's larger than or equal to desired size.

        Allocation closest = new Allocation();
        var closestIndex = -1;
        var closestMagnitude = -1;
        for (int i = 0; i <= _unused.Last; i++)
        {
            if (!_unused.Filled[i]) continue;
            var allocation = _unused.Items[i];
            if (allocation.Split.x > split.x || allocation.Split.y > split.y || allocation.Split.z > split.z) continue;
            var magnitude = allocation.Split.x + allocation.Split.y + allocation.Split.z;
            if (closestIndex >= 0 && magnitude <= closestMagnitude) continue;
            closest = allocation;
            closestIndex = i;
            closestMagnitude = magnitude;
        }

        if (closestIndex == -1)
        {
            throw new VolumeFullException(this, split);
        }

        _unused.RemoveAt(closestIndex);

        #endregion

        #region Split up allocation until it matches desired size, enlist new allocations from splits.

        while (closest.Split.x < split.x)
        {
            closest.Split.x++;
            closest.Offset.x *= 2;
            var newAllocation = closest.Clone();
            newAllocation.Offset.x += 1;
            _unused.Add(newAllocation);
        }

        while (closest.Split.y < split.y)
        {
            closest.Split.y++;
            closest.Offset.y *= 2;
            var newAllocation = closest.Clone();
            newAllocation.Offset.y += 1;
            _unused.Add(newAllocation);
        }

        while (closest.Split.z < split.z)
        {
            closest.Split.z++;
            closest.Offset.z *= 2;
            var newAllocation = closest.Clone();
            newAllocation.Offset.z += 1;
            _unused.Add(newAllocation);
        }

        #endregion

        return closest;
    }

    public void Return(Allocation returning)
    {
        #region Search for equal sized allocation in unused that shares the same location to combine

        var combining = true;
        while (combining)
        {
            combining = false;
            for (int i = 0; i <= _unused.Last; i++)
            {
                if (!_unused.Filled[i]) continue;
                var allocation = _unused.Items[i];
                if (allocation.Split.x != returning.Split.x || allocation.Split.y != returning.Split.y || allocation.Split.z != returning.Split.z) continue;
                if (allocation.Offset.x / 2 == returning.Offset.x / 2 && allocation.Offset.y == returning.Offset.y && allocation.Offset.z == returning.Offset.z)
                {
                    _unused.RemoveAt(i);
                    returning.Split.x--;
                    returning.Offset.x /= 2;
                    combining = true;
                    break;
                }
                if (allocation.Offset.y / 2 == returning.Offset.y / 2 && allocation.Offset.x == returning.Offset.x && allocation.Offset.z == returning.Offset.z)
                {
                    _unused.RemoveAt(i);
                    returning.Split.y--;
                    returning.Offset.y /= 2;
                    combining = true;
                    break;
                }
                if (allocation.Offset.z / 2 == returning.Offset.z / 2 && allocation.Offset.y == returning.Offset.y && allocation.Offset.x == returning.Offset.x)
                {
                    _unused.RemoveAt(i);
                    returning.Split.z--;
                    returning.Offset.z /= 2;
                    combining = true;
                    break;
                }
            }
        }

        #endregion

        _unused.Add(returning);
    }
}
