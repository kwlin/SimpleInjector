﻿#region Copyright (c) 2010 S. van Deursen
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (C) 2010 S. van Deursen
 * 
 * To contact me, please visit my blog at http://www.cuttingedge.it/blogs/steven/ or mail to steven at 
 * cuttingedge.it.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace SimpleInjector.Extensions.Decorators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using SimpleInjector.Advanced;
    using SimpleInjector.Lifestyles;

    // A decoratable enumerable is a collection that holds a set of Expression objects. When a decorator is
    // applied to a collection, a new DecoratableEnumerable will be created
    internal sealed class ContainerControlledCollection<TService> : IndexableEnumerable<TService>, 
        IContainerControlledCollection
    {
        private readonly Container container;

        private List<Lazy<InstanceProducer>> producers;
        private DecoratorPredicateContext[] contexts;
        private Func<TService>[] instanceCreators;

        // This constructor needs to be public. It is called using reflection.
        public ContainerControlledCollection(Container container, Type[] serviceTypes)
        {
            this.container = container;
            this.producers = serviceTypes.Select(this.GetLazyInstanceProducer).ToList();
        }

        internal ContainerControlledCollection(Container container, TService[] singletons)
            : this(container, ConvertSingletonsToInstanceProducers(container, singletons))
        {
        }

        internal ContainerControlledCollection(Container container,
            IEnumerable<InstanceProducer> producers)
        {
            this.container = container;
            this.producers = producers.Select(Helpers.ToLazy).ToList();
        }

        public override int Count
        {
            get { return this.producers != null ? this.producers.Count : this.contexts.Length; }
        }

        public override TService this[int index]
        {
            get
            {
                this.InitializeInstanceCreators();

                return this.instanceCreators[index]();
            }

            set
            {
                throw IndexableEnumerable<TService>.GetNotSupportedBecauseCollectionIsReadOnlyException();
            }
        }

        KnownRelationship[] IContainerControlledCollection.GetRelationships()
        {
            return (
                from context in this.contexts ?? Enumerable.Empty<DecoratorPredicateContext>()
                from relationship in context.InstanceProducer.GetRelationships()
                select relationship)
                .Distinct()
                .ToArray();
        }

        public override IEnumerator<TService> GetEnumerator()
        {
            this.InitializeInstanceCreators();

            return this.GetEnumeratorForCreators();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void InitializeInstanceCreators()
        {
            if (this.instanceCreators == null)
            {
                this.instanceCreators = this.BuildInstanceCreators();
            }
        }

        private Func<TService>[] BuildInstanceCreators()
        {
            this.BuildContexts();

            return (
                from context in this.contexts
                select this.BuildInstanceCreator(context.Expression))
                .ToArray();
        }

        private Func<TService> BuildInstanceCreator(Expression expression)
        {
            Func<object> instanceCreator = null;

            return () =>
            {
                if (instanceCreator == null)
                {
                    object instance;

                    instanceCreator = Helpers.CompileAndRun(this.container, expression, out instance);

                    if (instance != null)
                    {
                        return (TService)instance;
                    }
                }

                return (TService)instanceCreator();
            };
        }

        private void BuildContexts()
        {
            if (this.contexts == null)
            {
                this.contexts = (
                    from producer in this.producers.Select(p => p.Value)
                    select DecoratorPredicateContext.CreateFromProducer(this.container, producer))
                    .ToArray();
            }
        }

        private IEnumerator<TService> GetEnumeratorForCreators()
        {
            foreach (var instanceCreator in this.instanceCreators)
            {
                yield return instanceCreator();
            }
        }
        
        private static IEnumerable<InstanceProducer> ConvertSingletonsToInstanceProducers(Container container,
            TService[] singletons)
        {
            return
                from instance in singletons
                let registration =
                    SingletonLifestyle.CreateSingleRegistration(typeof(TService), instance, container)
                select new InstanceProducer(typeof(TService), registration);
        }

        private Lazy<InstanceProducer> GetLazyInstanceProducer(Type serviceType)
        {
            // precondition: typeof(TService).IsAssignableFrom(serviceType).
            return new Lazy<InstanceProducer>(() =>
            {
                // instanceProducer.ServiceType == serviceType
                var instanceProducer = this.container.GetRegistration(serviceType, throwOnFailure: true);

                // We need to create a new InstanceProducer with instanceProducer.ServiceType == typeof(TService).
                // This allows decorators to be applied 
                return new InstanceProducer(typeof(TService), instanceProducer.Registration);
            });
        }
    }
}