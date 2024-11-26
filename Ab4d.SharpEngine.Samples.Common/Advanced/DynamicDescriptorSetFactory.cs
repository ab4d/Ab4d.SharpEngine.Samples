using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System;
using System.Collections.Generic;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

sealed class DynamicDescriptorSetFactory : GpuDeviceComponent
{
    sealed class DescriptorPoolItem
    {
        public readonly int Index;
        public readonly DescriptorPool DescriptorPool;
        public readonly int Capacity;
        public int Count;

        public int Available => Capacity - Count;

        public DescriptorPoolItem(int index, DescriptorPool descriptorPool, int capacity)
        {
            Index = index;
            DescriptorPool = descriptorPool;
            Capacity = capacity;
            Count = 0;
        }
    }

    readonly DescriptorType[] _descriptorTypes;
    readonly DescriptorSetLayout _descriptorSetLayout;
    int _poolCapacity;
    readonly int _maxPoolCapacity;

    readonly List<DescriptorPoolItem> _descriptorPools;
    DescriptorPoolItem _currentDescriptorPool;

    DynamicDescriptorSetFactory(
        VulkanDevice vulkanDevice,
        DescriptorType[] descriptorTypes,
        DescriptorSetLayout descriptorSetLayout,
        int initialPoolCapacity = 16,
        int maxPoolCapacity = 512,
        string? name = null)
        : base(vulkanDevice, name)
    {
        _descriptorTypes = descriptorTypes;
        _descriptorSetLayout = descriptorSetLayout;
        _poolCapacity = initialPoolCapacity;
        _maxPoolCapacity = maxPoolCapacity;

        _descriptorPools = new();
        _currentDescriptorPool = _createDescriptorPool();
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (GpuDevice.IsDisposed)
            return;
        lock (this)
        {
            foreach (var descriptorPoolItem in _descriptorPools)
                GpuDevice.Vk.DestroyDescriptorPool(GpuDevice.Device, descriptorPoolItem.DescriptorPool, null);
            _descriptorPools.Clear();
        }
    }

    public static (DynamicDescriptorSetFactory, DisposeToken) Create(
        VulkanDevice vulkanDevice,
        DescriptorType[] descriptorTypes,
        DescriptorSetLayout descriptorSetLayout,
        int initialPoolCapacity = 16,
        int maxPoolCapacity = 512,
        string? name = null)
    {
        var obj = new DynamicDescriptorSetFactory(vulkanDevice, descriptorTypes, descriptorSetLayout, initialPoolCapacity, maxPoolCapacity, name);
        var disposeToken = new DisposeToken(() => obj.CheckAndDispose(true));
        return (obj, disposeToken);
    }

    public (DescriptorSet[] descriptorSets, int poolIndex) CreateDescriptorSets(int count, string? name = null)
    {
        GpuDevice.CheckIsOnMainThread();
        DescriptorSet[] descriptorSets;
        lock (this)
        {
            if (_currentDescriptorPool.Available < count)
            {
                var maxAvailable = 0;
                var maxAvailableIndex = -1;
                for (var i = 0; i < _descriptorPools.Count; ++i)
                {
                    if (_descriptorPools[i].Available > maxAvailable)
                    {
                        maxAvailable = _descriptorPools[i].Available;
                        maxAvailableIndex = i;
                    }
                }
                _currentDescriptorPool = maxAvailable < count ? _createDescriptorPool() : _descriptorPools[maxAvailableIndex];
            }
            descriptorSets = GpuDevice.CreateDescriptorSets(_descriptorSetLayout, _currentDescriptorPool.DescriptorPool, count, name);
            _currentDescriptorPool.Count += count;
        }
        return (descriptorSets, _currentDescriptorPool.Index);
    }

    DescriptorPoolItem _createDescriptorPool()
    {
        lock (this)
        {
            var count = _descriptorPools.Count;
            var descriptorPool = GpuDevice.CreateDescriptorPool(_descriptorTypes, _poolCapacity, true, $"{Name}-Pool{count}");
            var newPool = new DescriptorPoolItem(count, descriptorPool, _poolCapacity);
            _descriptorPools.Add(newPool);
            _poolCapacity = Math.Min(_maxPoolCapacity, _poolCapacity * 2);
            return newPool;
        }
    }

    public void DestroyDescriptorSets(DescriptorSet[] descriptorSets, int poolIndex)
    {
        ArgumentNullException.ThrowIfNull(descriptorSets);
        if (IsDisposed)
            return;
        GpuDevice.ExecuteOnMainThreadAfterFrameRendered(() => _destroyDescriptorSets(descriptorSets, poolIndex));
    }

    unsafe void _destroyDescriptorSets(DescriptorSet[] descriptorSets, int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= _descriptorPools.Count)
            throw new ArgumentOutOfRangeException(nameof(poolIndex));
        lock (this)
        {
            var descriptorPoolItem = _descriptorPools[poolIndex];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
                GpuDevice.Vk.FreeDescriptorSets(GpuDevice.Device, descriptorPoolItem.DescriptorPool, (uint)descriptorSets.Length, descriptorSetsPtr);
            descriptorPoolItem.Count -= descriptorSets.Length;
            if (_currentDescriptorPool.Available < descriptorPoolItem.Available)
                _currentDescriptorPool = descriptorPoolItem;
        }
    }
}
