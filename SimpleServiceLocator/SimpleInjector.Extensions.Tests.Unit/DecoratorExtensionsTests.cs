﻿namespace SimpleInjector.Extensions.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DecoratorExtensionsTests
    {
        public interface ILogger
        {
            void Log(string message);
        }

        public interface ICommandHandler<TCommand>
        {
            void Handle(TCommand command);
        }

        public interface INonGenericService
        {
            void DoSomething();
        }

        [TestMethod]
        public void GetInstance_OnDecoratedNonGenericType_ReturnsTheDecoratedService()
        {
            // Arrange
            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator));

            var decorator = (NonGenericServiceDecorator)service;

            Assert.IsInstanceOfType(decorator.DecoratedService, typeof(RealNonGenericService));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedNonGenericType_DecoratesInstanceWithExpectedLifeTime()
        {
            // Arrange
            var container = new Container();

            // Register as transient
            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var decorator1 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();
            var decorator2 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsFalse(object.ReferenceEquals(decorator1.DecoratedService, decorator2.DecoratedService),
                "The decorated instance is expected to be a transient.");
        }

        [TestMethod]
        public void RegisterDecorator_RegisteringAnOpenGenericDecoratorWithANonGenericService_ThrowsExpectedException()
        {
            // Arrange
            var container = new Container();

            try
            {
                // Act
                container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator<>));

                Assert.Fail("Exception was expected.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains(@"
                    The supplied decorator 'DecoratorExtensionsTests+NonGenericServiceDecorator<T>' is an open
                    generic type definition".TrimInside(),
                    ex.Message);

                AssertThat.ExceptionContainsParamName("decoratorType", ex);
            }
        }

        [TestMethod]
        public void GetInstance_OnNonGenericTypeDecoratedWithGenericDecorator_ReturnsTheDecoratedService()
        {
            // Arrange
            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator<int>));

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator<int>));

            var decorator = (NonGenericServiceDecorator<int>)service;

            Assert.IsInstanceOfType(decorator.DecoratedService, typeof(RealNonGenericService));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_ReturnsTheDecorator()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(RealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatMatchesTheRequestedService1_ReturnsTheDecorator()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<RealCommand>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void RegisterDecorator_WithClosedGenericServiceAndOpenGenericDecorator_FailsWithExpectedException()
        {
            // Arrange
            string expectedMessage = @"
                Registering a closed generic service type with an open generic decorator is not supported. 
                Instead, register the service type as open generic, and the decorator as closed generic 
                type."
                .TrimInside();

            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            try
            {
                // Act
                container.RegisterDecorator(
                    typeof(ICommandHandler<RealCommand>),
                    typeof(TransactionHandlerDecorator<>));

                // Assert
                Assert.Fail("Exception excepted.");
            }
            catch (NotSupportedException ex)
            {
                AssertThat.ExceptionMessageContains(expectedMessage, ex);
            }
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatMatchesTheRequestedService2_ReturnsTheDecorator()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(
                typeof(ICommandHandler<RealCommand>),
                typeof(TransactionHandlerDecorator<RealCommand>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatDoesNotMatchTheRequestedService1_ReturnsTheServiceItself()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<int>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandler));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatDoesNotMatchTheRequestedService2_ReturnsTheServiceItself()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<int>), typeof(TransactionHandlerDecorator<int>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandler));
        }

        [TestMethod]
        public void GetInstance_NonGenericDecoratorForMatchingClosedGenericServiceType_ReturnsTheNonGenericDecorator()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            Type closedGenericServiceType = typeof(ICommandHandler<RealCommand>);
            Type nonGenericDecorator = typeof(RealCommandHandlerDecorator);

            container.RegisterDecorator(closedGenericServiceType, nonGenericDecorator);

            // Act
            var decorator = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(decorator, nonGenericDecorator);
        }

        [TestMethod]
        public void GetInstance_NonGenericDecoratorForNonMatchingClosedGenericServiceType_ThrowsAnException()
        {
            // Arrange
            var container = new Container();

            Type nonMathcingClosedGenericServiceType = typeof(ICommandHandler<int>);

            // Decorator implements ICommandHandler<RealCommand>
            Type nonGenericDecorator = typeof(RealCommandHandlerDecorator);

            try
            {
                // Act
                container.RegisterDecorator(nonMathcingClosedGenericServiceType, nonGenericDecorator);

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains(
                    "The supplied type 'DecoratorExtensionsTests+RealCommandHandlerDecorator' does not " +
                    "inherit from or implement 'DecoratorExtensionsTests+ICommandHandler<Int32>'", ex.Message);
            }
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_GetsHandledAsExpected()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = new Container();

            container.RegisterSingle<ILogger>(logger);

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(LoggingRealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>().Handle(new RealCommand());

            // Assert
            Assert.AreEqual("Begin1 RealCommand End1", logger.Message);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_ReturnsLastRegisteredDecorator()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(RealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LogExceptionCommandHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(LogExceptionCommandHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_GetsHandledAsExpected()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = new Container();

            container.RegisterSingle<ILogger>(logger);

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(LoggingRealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator2<>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>().Handle(new RealCommand());

            // Assert
            Assert.AreEqual("Begin2 Begin1 RealCommand End1 End2", logger.Message);
        }

        [TestMethod]
        public void GetInstance_WithInitializerOnDecorator_InitializesThatDecorator()
        {
            // Arrange
            int expectedItem1Value = 1;
            string expectedItem2Value = "some value";

            var container = new Container();

            container.RegisterInitializer<HandlerDecoratorWithPropertiesBase>(decorator =>
            {
                decorator.Item1 = expectedItem1Value;
            });

            container.RegisterInitializer<HandlerDecoratorWithPropertiesBase>(decorator =>
            {
                decorator.Item2 = expectedItem2Value;
            });

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(HandlerDecoratorWithProperties<>));

            // Act
            var handler =
                (HandlerDecoratorWithPropertiesBase)container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedItem1Value, handler.Item1, "Initializer did not run.");
            Assert.AreEqual(expectedItem2Value, handler.Item2, "Initializer did not run.");
        }

        [TestMethod]
        public void GetInstance_DecoratorWithMissingDependency_ThrowAnExceptionWithADescriptiveMessage()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            // Decorator1Handler depends on ILogger, but ILogger is not registered.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            try
            {
                // Act
                var handler = container.GetInstance<ICommandHandler<RealCommand>>();

                // Assert
                Assert.Fail("Exception was expected.");
            }
            catch (ActivationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ILogger"), "Actual message: " + ex.Message);
            }
        }

        [TestMethod]
        public void GetInstance_DecoratorPredicateReturnsFalse_DoesNotDecorateInstance()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c => false);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StubCommandHandler));
        }

        [TestMethod]
        public void GetInstance_DecoratorPredicateReturnsTrue_DecoratesInstance()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>),
                c => true);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_CallsThePredicateWithTheExpectedServiceType()
        {
            // Arrange
            Type expectedPredicateServiceType = typeof(ICommandHandler<RealCommand>);
            Type actualPredicateServiceType = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateServiceType = c.ServiceType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateServiceType, actualPredicateServiceType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTransient_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTransientWithInitializer_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterInitializer<StubCommandHandler>(handlerToInitialize => { });

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_SingletonDecoratorWithInitializer_ShouldReturnSingleton()
        {
            // Arrange
            var container = new Container();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            container.RegisterSingleDecorator(typeof(ICommandHandler<>), typeof(AsyncCommandHandlerProxy<>));

            container.RegisterInitializer<AsyncCommandHandlerProxy<RealCommand>>(handler => { });

            // Act
            var handler1 = container.GetInstance<ICommandHandler<RealCommand>>();
            var handler2 = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(AsyncCommandHandlerProxy<RealCommand>));

            Assert.IsTrue(object.ReferenceEquals(handler1, handler2),
                "GetInstance should always return the same instance, since AsyncCommandHandlerProxy is " +
                "registered as singleton.");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedSingleton_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = new Container();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new StubCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTypeRegisteredWithFuncDelegate_CallsThePredicateWithTheImplementationTypeEqualsServiceType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(ICommandHandler<RealCommand>);
            Type actualPredicateImplementationType = null;

            var container = new Container();

            // Because we registere a Func<TServiceType> there is no way we can determine the implementation 
            // type. In that case the ImplementationType should equal the ServiceType.
            container.Register<ICommandHandler<RealCommand>>(() => new StubCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_CallsThePredicateWithAnExpression()
        {
            // Arrange
            Expression actualPredicateExpression = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateExpression = c.Expression;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsNotNull(actualPredicateExpression);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_SuppliesADifferentExpressionToTheSecondPredicate()
        {
            // Arrange
            Expression predicateExpressionOnFirstCall = null;
            Expression predicateExpressionOnSecondCall = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                predicateExpressionOnFirstCall = c.Expression;
                return true;
            });

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
            {
                predicateExpressionOnSecondCall = c.Expression;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreNotEqual(predicateExpressionOnFirstCall, predicateExpressionOnSecondCall,
                "The predicate was expected to change, because the first decorator has been applied.");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
            {
                appliedDecorators = c.AppliedDecorators;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(0, appliedDecorators.Count());
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances1_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
            {
                appliedDecorators = c.AppliedDecorators;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(1, appliedDecorators.Count());
            Assert.AreEqual(typeof(TransactionHandlerDecorator<RealCommand>), appliedDecorators.First());
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances2_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
            {
                appliedDecorators = c.AppliedDecorators;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(2, appliedDecorators.Count());
            Assert.AreEqual(typeof(TransactionHandlerDecorator<RealCommand>), appliedDecorators.First());
            Assert.AreEqual(typeof(LogExceptionCommandHandlerDecorator<RealCommand>), appliedDecorators.Second());
        }

        [TestMethod]
        public void GetInstance_DecoratorThatSatisfiesRequestedTypesTypeConstraints_DecoratesThatInstance()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(ClassConstraintHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(ClassConstraintHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_DecoratorThatDoesNotSatisfyRequestedTypesTypeConstraints_DoesNotDecorateThatInstance()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StructCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(ClassConstraintHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<StructCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StructCommandHandler));
        }

        [TestMethod]
        public void RegisterDecorator_DecoratorWithMultiplePublicConstructors_ThrowsException()
        {
            // Arrange
            var container = new Container();

            try
            {
                // Act
                container.RegisterDecorator(typeof(ICommandHandler<>),
                    typeof(MultipleConstructorsCommandHandlerDecorator<>));

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains("it should contain exactly one public constructor", ex.Message);
            }
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingTypeThatIsNotADecorator_ThrowsException()
        {
            // Arrange
            var container = new Container();

            try
            {
                // Act
                container.RegisterDecorator(typeof(ICommandHandler<>),
                    typeof(InvalidDecoratorCommandHandlerDecorator<>));

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains(@"
                    its constructor should have a single argument of type 
                    DecoratorExtensionsTests+ICommandHandler<TCommand>".TrimInside(),
                    ex.Message);
            }
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingAnUnrelatedType_FailsWithExpectedException()
        {
            // Arrange
            var container = new Container();

            try
            {
                // Act
                container.RegisterDecorator(typeof(ICommandHandler<>), typeof(KeyValuePair<,>));
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains(
                    "The supplied type 'KeyValuePair<TKey, TValue>' does not inherit from " +
                    "or implement 'DecoratorExtensionsTests+ICommandHandler<TCommand>'.",
                    ex.Message);
            }
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingAConcreteNonGenericType_ShouldSucceed()
        {
            // Arrange
            var container = new Container();

            // Act
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericType_ReturnsExpectedDecorator1()
        {
            // Arrange
            var container = new Container();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericTypeThatDoesNotMatch_DoesNotReturnThatDecorator()
        {
            // Arrange
            var container = new Container();

            // StructCommandHandler implements ICommandHandler<StructCommand>
            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StructCommandHandler));

            // ConcreteCommandHandlerDecorator implements ICommandHandler<RealCommand>
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<StructCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StructCommandHandler));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericType_ReturnsExpectedDecorator2()
        {
            // Arrange
            var container = new Container();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();
            container.Register<ICommandHandler<StructCommand>, StructCommandHandler>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void RegisterDecorator_NonGenericDecoratorWithFuncAsConstructorArgument_InjectsAFactoryThatCreatesNewInstancesOfTheDecoratedType()
        {
            // Arrange
            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecoratorWithFunc));

            var decorator = (NonGenericServiceDecoratorWithFunc)container.GetInstance<INonGenericService>();

            Func<INonGenericService> factory = decorator.DecoratedServiceCreator;

            // Act
            // Execute the factory twice.
            INonGenericService instance1 = factory();
            INonGenericService instance2 = factory();

            // Assert
            Assert.IsInstanceOfType(instance1, typeof(RealNonGenericService),
                "The injected factory is expected to create instances of type RealNonGenericService.");

            Assert.IsFalse(object.ReferenceEquals(instance1, instance2),
                "The factory is expected to create transient instances, since that is how " +
                "RealNonGenericService is registered.");
        }

        [TestMethod]
        public void RegisterDecorator_GenericDecoratorWithFuncAsConstructorArgument_InjectsAFactoryThatCreatesNewInstancesOfTheDecoratedType()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<ILogger>(new FakeLogger());

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LogExceptionCommandHandlerDecorator<>));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(AsyncCommandHandlerProxy<>));

            // Act
            var handler =
                (AsyncCommandHandlerProxy<RealCommand>)container.GetInstance<ICommandHandler<RealCommand>>();

            Func<ICommandHandler<RealCommand>> factory = handler.DecorateeFactory;

            // Execute the factory twice.
            ICommandHandler<RealCommand> instance1 = factory();
            ICommandHandler<RealCommand> instance2 = factory();

            // Assert
            Assert.IsInstanceOfType(instance1, typeof(LogExceptionCommandHandlerDecorator<RealCommand>),
                "The injected factory is expected to create instances of type " +
                "LogAndContinueCommandHandlerDecorator<RealCommand>.");

            Assert.IsFalse(object.ReferenceEquals(instance1, instance2),
                "The factory is expected to create transient instances.");
        }

        [TestMethod]
        public void RegisterDecorator_CalledWithDecoratorTypeWithBothAFuncAndADecorateeParameter_ThrowsExpectedException()
        {
            // Arrange
            var container = new Container();

            try
            {
                // Act
                container.RegisterDecorator(typeof(INonGenericService),
                    typeof(NonGenericServiceDecoratorWithBothDecorateeAndFunc));

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (ArgumentException ex)
            {
                AssertThat.StringContains("its constructor should have a single argument of type " +
                    "DecoratorExtensionsTests+INonGenericService or Func<DecoratorExtensionsTests+INonGenericService>",
                    ex.Message);
            }
        }

        [TestMethod]
        public void GetInstance_TypeRegisteredWithRegisterSingleDecorator_AlwaysReturnsTheSameInstance()
        {
            // Arrange
            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterSingleDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var decorator1 = container.GetInstance<INonGenericService>();
            var decorator2 = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(decorator1, typeof(NonGenericServiceDecorator));

            Assert.IsTrue(object.ReferenceEquals(decorator1, decorator2),
                "Since the decorator is registered as singleton, GetInstance should always return the same " +
                "instance.");
        }

        [TestMethod]
        public void GetInstance_TypeRegisteredWithRegisterSingleDecoratorPredicate_AlwaysReturnsTheSameInstance()
        {
            // Arrange
            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterSingleDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator),
                c => true);

            // Act
            var decorator1 = container.GetInstance<INonGenericService>();
            var decorator2 = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(decorator1, typeof(NonGenericServiceDecorator));

            Assert.IsTrue(object.ReferenceEquals(decorator1, decorator2),
                "Since the decorator is registered as singleton, GetInstance should always return the same " +
                "instance.");
        }

        [TestMethod]
        public void Verify_DecoratorRegisteredThatCanNotBeResolved_ThrowsExpectedException()
        {
            // Arrange
            var container = new Container();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            // LoggingHandlerDecorator1 depends on ILogger, which is not registered.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            try
            {
                // Act
                container.Verify();

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.ExceptionMessageContains(@"
                    The constructor of the type 
                    DecoratorExtensionsTests+LoggingHandlerDecorator1<DecoratorExtensionsTests+RealCommand> 
                    contains the parameter of type DecoratorExtensionsTests+ILogger with name 'logger' that is 
                    not registered.".TrimInside(), ex);
            }
        }

        [TestMethod]
        public void GetInstance_DecoratorRegisteredTwiceAsSingleton_WrapsTheDecorateeTwice()
        {
            // Arrange
            var container = new Container();

            // Uses the RegisterAll<T>(IEnumerable<T>) that registers a dynamic list.
            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            // Register the same decorator twice. 
            container.RegisterSingleDecorator(
                typeof(ICommandHandler<>),
                typeof(TransactionHandlerDecorator<>));

            container.RegisterSingleDecorator(
                typeof(ICommandHandler<>),
                typeof(TransactionHandlerDecorator<>));

            // Act
            var decorator1 = (TransactionHandlerDecorator<RealCommand>)
                container.GetInstance<ICommandHandler<RealCommand>>();

            var decorator2 = decorator1.Decorated;

            // Assert
            Assert.IsInstanceOfType(decorator2, typeof(TransactionHandlerDecorator<RealCommand>),
                "Since the decorator is registered twice, it should wrap the decoratee twice.");

            var decoratee = ((TransactionHandlerDecorator<RealCommand>)decorator2).Decorated;

            Assert.IsInstanceOfType(decoratee, typeof(StubCommandHandler));
        }

        public struct StructCommand
        {
        }

        public sealed class FakeLogger : ILogger
        {
            public string Message { get; private set; }

            public void Log(string message)
            {
                this.Message += message;
            }
        }

        public class RealCommand
        {
        }

        public class MultipleConstructorsCommandHandlerDecorator<T> : ICommandHandler<T>
        {
            public MultipleConstructorsCommandHandlerDecorator()
            {
            }

            public MultipleConstructorsCommandHandlerDecorator(ICommandHandler<T> decorated)
            {
            }

            public void Handle(T command)
            {
            }
        }

        public class InvalidDecoratorCommandHandlerDecorator<T> : ICommandHandler<T>
        {
            // This is no decorator, since it lacks the ICommandHandler<T> parameter.
            public InvalidDecoratorCommandHandlerDecorator(ILogger logger)
            {
            }

            public void Handle(T command)
            {
            }
        }

        public class StubCommandHandler : ICommandHandler<RealCommand>
        {
            public void Handle(RealCommand command)
            {
            }
        }

        public class StructCommandHandler : ICommandHandler<StructCommand>
        {
            public void Handle(StructCommand command)
            {
            }
        }

        public class RealCommandHandler : ICommandHandler<RealCommand>
        {
            public void Handle(RealCommand command)
            {
            }
        }

        public class LoggingRealCommandHandler : ICommandHandler<RealCommand>
        {
            private readonly ILogger logger;

            public LoggingRealCommandHandler(ILogger logger)
            {
                this.logger = logger;
            }

            public void Handle(RealCommand command)
            {
                this.logger.Log("RealCommand");
            }
        }

        public class RealCommandHandlerDecorator : ICommandHandler<RealCommand>
        {
            public RealCommandHandlerDecorator(ICommandHandler<RealCommand> decorated)
            {
                this.Decorated = decorated;
            }

            public ICommandHandler<RealCommand> Decorated { get; private set; }

            public void Handle(RealCommand command)
            {
            }
        }

        public class TransactionHandlerDecorator<T> : ICommandHandler<T>
        {
            public TransactionHandlerDecorator(ICommandHandler<T> decorated)
            {
                this.Decorated = decorated;
            }

            public ICommandHandler<T> Decorated { get; private set; }

            public void Handle(T command)
            {
            }
        }

        public class LogExceptionCommandHandlerDecorator<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> decorated;

            public LogExceptionCommandHandlerDecorator(ICommandHandler<T> decorated)
            {
                this.decorated = decorated;
            }

            public void Handle(T command)
            {
                // called the decorated instance and log any exceptions (not important for these tests).
            }
        }

        public class LoggingHandlerDecorator1<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> wrapped;
            private readonly ILogger logger;

            public LoggingHandlerDecorator1(ICommandHandler<T> wrapped, ILogger logger)
            {
                this.wrapped = wrapped;
                this.logger = logger;
            }

            public void Handle(T command)
            {
                this.logger.Log("Begin1 ");
                this.wrapped.Handle(command);
                this.logger.Log(" End1");
            }
        }

        public class LoggingHandlerDecorator2<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> wrapped;
            private readonly ILogger logger;

            public LoggingHandlerDecorator2(ICommandHandler<T> wrapped, ILogger logger)
            {
                this.wrapped = wrapped;
                this.logger = logger;
            }

            public void Handle(T command)
            {
                this.logger.Log("Begin2 ");
                this.wrapped.Handle(command);
                this.logger.Log(" End2");
            }
        }

        public class AsyncCommandHandlerProxy<T> : ICommandHandler<T>
        {
            public AsyncCommandHandlerProxy(Container container, Func<ICommandHandler<T>> decorateeFactory)
            {
                this.DecorateeFactory = decorateeFactory;
            }

            public Func<ICommandHandler<T>> DecorateeFactory { get; private set; }

            public void Handle(T command)
            {
                // Run decorated instance on new thread (not important for these tests).
            }
        }

        public class LifetimeScopeCommandHandlerProxy<T> : ICommandHandler<T>
        {
            public LifetimeScopeCommandHandlerProxy(Func<ICommandHandler<T>> decorateeFactory,
                Container container)
            {
                this.DecorateeFactory = decorateeFactory;
            }

            public Func<ICommandHandler<T>> DecorateeFactory { get; private set; }

            public void Handle(T command)
            {
                // Start lifetime scope here (not important for these tests).
            }
        }

        public class ClassConstraintHandlerDecorator<T> : ICommandHandler<T> where T : class
        {
            public ClassConstraintHandlerDecorator(ICommandHandler<T> wrapped)
            {
            }

            public void Handle(T command)
            {
            }
        }

        public class HandlerDecoratorWithPropertiesBase
        {
            public int Item1 { get; set; }

            public string Item2 { get; set; }
        }

        public class HandlerDecoratorWithProperties<T> : HandlerDecoratorWithPropertiesBase, ICommandHandler<T>
        {
            private readonly ICommandHandler<T> wrapped;

            public HandlerDecoratorWithProperties(ICommandHandler<T> wrapped)
            {
                this.wrapped = wrapped;
            }

            public void Handle(T command)
            {
            }
        }

        public class RealNonGenericService : INonGenericService
        {
            public void DoSomething()
            {
            }
        }

        public class NonGenericServiceDecorator : INonGenericService
        {
            public NonGenericServiceDecorator(INonGenericService decorated)
            {
                this.DecoratedService = decorated;
            }

            public INonGenericService DecoratedService { get; private set; }

            public void DoSomething()
            {
                this.DecoratedService.DoSomething();
            }
        }

        public class NonGenericServiceDecorator<T> : INonGenericService
        {
            public NonGenericServiceDecorator(INonGenericService decorated)
            {
                this.DecoratedService = decorated;
            }

            public INonGenericService DecoratedService { get; private set; }

            public void DoSomething()
            {
                this.DecoratedService.DoSomething();
            }
        }

        public class NonGenericServiceDecoratorWithFunc : INonGenericService
        {
            public NonGenericServiceDecoratorWithFunc(Func<INonGenericService> decoratedCreator)
            {
                this.DecoratedServiceCreator = decoratedCreator;
            }

            public Func<INonGenericService> DecoratedServiceCreator { get; private set; }

            public void DoSomething()
            {
                this.DecoratedServiceCreator().DoSomething();
            }
        }

        public class NonGenericServiceDecoratorWithBothDecorateeAndFunc : INonGenericService
        {
            public NonGenericServiceDecoratorWithBothDecorateeAndFunc(INonGenericService decoratee,
                Func<INonGenericService> decoratedCreator)
            {
            }

            public void DoSomething()
            {
            }
        }
    }
}