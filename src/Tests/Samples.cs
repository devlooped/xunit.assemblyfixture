﻿using System;
using System.Threading.Tasks;

namespace Xunit
{
    public class Sample1 : IAssemblyFixture<MyAssemblyFixture>
    {
        MyAssemblyFixture fixture;

        // Fixtures are injectable into the test classes, just like with class and collection fixtures
        public Sample1(MyAssemblyFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void EnsureSingleton()
        {
            Assert.Equal(1, MyAssemblyFixture.InstantiationCount);
        }
    }

    public class Sample2 : IAssemblyFixture<MyAssemblyFixture>
    {
        MyAssemblyFixture fixture;

        public Sample2(MyAssemblyFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void EnsureSingleton()
        {
            Assert.Equal(1, MyAssemblyFixture.InstantiationCount);
        }
    }
    
    public class Sample3
    {
        [Fact]
        public void EnsureSingleton()
        {
            Assert.Equal(1, MyAssemblyAsyncLifetime.InitializationCount);
        }
    }

    public class MyAssemblyFixture : IDisposable
    {
        public static int InstantiationCount;

        public MyAssemblyFixture()
        {
            InstantiationCount++;
        }

        public void Dispose()
        {
            // Uncomment this and it will surface as an assembly cleanup failure
            //throw new DivideByZeroException();
            //InstantiationCount = 0;
        }
    }

    public class MyAssemblyAsyncLifetime : IAssemblyAsyncLifetime
    {
        public static int InitializationCount;

        public Task InitializeAsync()
        {
            InitializationCount++;
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}