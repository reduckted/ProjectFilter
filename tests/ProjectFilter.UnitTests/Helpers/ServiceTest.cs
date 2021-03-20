using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Moq;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Helpers {

    [Collection(VisualStudioTests.Name)]
    public class ServiceTest<T> : IAsyncServiceProvider {

        [SuppressMessage("Reliability", "VSSDK005:Avoid instantiating JoinableTaskContext", Justification = "Testing.")]
        private static readonly JoinableTaskContext Context = new JoinableTaskContext();


        private readonly Dictionary<string, object> _services = new Dictionary<string, object>();


        static ServiceTest() {
            ExtensionThreadHelper.JoinableTaskFactory = Context.Factory;

            // Work around the `ThreadHelper.ThrowIfNotOnUIThread()` always throwing in 
            // tests by setting the internal dispatcher that is used to check for access.
            typeof(ThreadHelper).InvokeMember(
                "uiThreadDispatcher",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField,
                null,
                null,
                new[] { Dispatcher.CurrentDispatcher },
                CultureInfo.InvariantCulture
            );
        }


        protected ServiceTest() {
            AddService<ILogger, ILogger>(Mock.Of<ILogger>());
        }


        protected void AddService<TInterface, TImplementation>(TImplementation service) where TImplementation : class {
            _services[typeof(TInterface).FullName] = service;
        }


        protected async Task<T> CreateAsync() {
            T service;


            service = (T)Activator.CreateInstance(typeof(T), this);

            if (service is IAsyncInitializable initializable) {
                await initializable.InitializeAsync(CancellationToken.None);
            }

            return service;
        }


        public Task<object> GetServiceAsync(Type serviceType) {
            if (serviceType is null) {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (_services.TryGetValue(serviceType.FullName, out object service)) {
                return Task.FromResult(service);
            }

            throw new NotSupportedException($"Service '{serviceType.Name}' has not been registered.");
        }

    }

}