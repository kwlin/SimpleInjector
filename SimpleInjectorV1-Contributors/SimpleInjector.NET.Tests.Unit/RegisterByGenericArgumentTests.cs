﻿namespace SimpleInjector.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;
    
    [TestFixture]
    public class RegisterByGenericArgumentTests
    {
        [Test]
        public void RegisterByGenericArgument_WithValidGenericArguments_Succeeds()
        {
            // Arrange
            var container = new Container();

            // Act
            container.Register<IUserRepository, SqlUserRepository>();
        }

        [Test]
        public void GetInstance_OnRegisteredType_ReturnsInstanceOfExpectedType()
        {
            // Arrange
            var container = new Container();

            container.Register<IUserRepository, SqlUserRepository>();

            // Act
            var instance = container.GetInstance<IUserRepository>();

            // Assert
            Assert.IsInstanceOf<SqlUserRepository>(instance);
        }

        [Test]
        public void GetInstance_OnRegisteredType_ReturnsANewInstanceOnEachCall()
        {
            // Arrange
            var container = new Container();

            container.Register<IUserRepository, InMemoryUserRepository>();

            // Act
            var instance1 = container.GetInstance<IUserRepository>();
            var instance2 = container.GetInstance<IUserRepository>();

            // Assert
            Assert.AreNotEqual(instance1, instance2, "Register<TService, TImplementation>() should " + 
                "return transient objects.");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void RegisterByGenericArgument_GenericArgumentOfInvalidType_ThrowsException()
        {
            // Arrange
            var container = new Container();

            // Act
            container.Register<object, ConcreteTypeWithValueTypeConstructorArgument>();
        }

        [Test]
        public void RegisterByGenericArgument_GenericArgumentOfInvalidType_ThrowsExceptionWithExpectedParamName()
        {
            // Arrange
            string expectedParamName = "TImplementation";

            var container = new Container();

            try
            {
                // Act
                container.Register<object, ConcreteTypeWithValueTypeConstructorArgument>();

                // Assert
                Assert.Fail("Registration of ConcreteTypeWithValueTypeConstructorArgument should fail.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.ExceptionContainsParamName(ex, expectedParamName);
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RegisterByGenericArgument_CalledAfterTheContainerWasLocked_ThrowsException()
        {
            // Arrange
            var container = new Container();

            container.GetInstance<PluginManager>();

            // Act
            container.Register<IUserRepository, SqlUserRepository>();
        }
    }
}