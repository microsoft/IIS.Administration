// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WindowsService {
    using System;
    using System.ComponentModel;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    

    sealed class ServiceHelper {
        private const uint ERROR_MORE_DATA = 0xEA;
        private const int PENDING_TIMEOUT = 4000; // Timeout in ms during starting/stopping

        private delegate int SvcCtrlHandlerEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);
        private delegate void SvcMainHandler(int argCount, IntPtr args);

        private string _serviceName;
        private Task _svcInitTask;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private IntPtr _serviceHandle = IntPtr.Zero;

        SvcMainHandler _svcMainHandler;
        SvcCtrlHandlerEx _svcCtrlHandlerEx;

        class TaskState {
            public ExceptionDispatchInfo Exception { get; set; }
        }

        public ServiceHelper(string serviceName) {
            if (string.IsNullOrEmpty(serviceName)) {
                throw new ArgumentNullException(nameof(serviceName));
            }

            _serviceName = serviceName;
            _svcMainHandler = new SvcMainHandler(SvcMain);
            _svcCtrlHandlerEx = new SvcCtrlHandlerEx(SvcCtrlHandler);
        }

        private bool IsService {
            get {
                return !string.IsNullOrEmpty(_serviceName);
            }
        }

        public string ServiceName {
            get {
                return _serviceName;
            }
        }

        public async Task Run(Action<CancellationToken> action) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            if (!IsService) {
                throw new InvalidOperationException("The process is not running as Windows Service");
            }

            await EnsureInit();

            await Task.Run(()=> action(_cancellationToken.Token));
        }

        private Task EnsureInit() {
            if (_svcInitTask != null) {
                return _svcInitTask;
            }

            _svcInitTask = CreateInitTask();

            //
            // Start StartServiceCtrlDispatcher
            Task.Run(() => {
                IntPtr namePtr = Marshal.StringToHGlobalUni(ServiceName);

                try {
                    //
                    // Build SERVICE_TABLE_ENTRY[2] table
                    IntPtr ptr = Marshal.AllocHGlobal(2 * Marshal.SizeOf<Interop.SERVICE_TABLE_ENTRY>());

                    Marshal.StructureToPtr(new Interop.SERVICE_TABLE_ENTRY() { callback = _svcMainHandler, name = namePtr }, ptr, true);
                    Marshal.StructureToPtr(new Interop.SERVICE_TABLE_ENTRY() { callback = null, name = IntPtr.Zero },
                                           new IntPtr(ptr.ToInt64() + Marshal.SizeOf<Interop.SERVICE_TABLE_ENTRY>()), true);

                    //
                    // Blocks until the Windows Service stops
                    if (!Interop.StartServiceCtrlDispatcherW(ptr)) {
                        throw new Win32Exception();
                    }
                }
                catch (Exception e) {
                    CompleteInitTask(e);
                    throw e;
                }
                finally {
                    Marshal.FreeHGlobal(namePtr);
                }
            });

            return _svcInitTask;
        }

        
        private void SvcMain(int argCount, IntPtr args) {
            try {
                //
                // Register the handler function for the service
                _serviceHandle = Interop.RegisterServiceCtrlHandlerExW(ServiceName, _svcCtrlHandlerEx, IntPtr.Zero);
                if (_serviceHandle == IntPtr.Zero) {
                    throw new Win32Exception();
                }

                //
                // 
                // Report Starting
                SetStatus(Interop.SERVICE_START_PENDING);


                //
                // Do some startup logic here...
                //


                //
                // 
                // Report Running
                SetStatus(Interop.SERVICE_RUNNING);


                //
                //
                CompleteInitTask(null);


                //
                // Wait for stop event
                _cancellationToken.Token.WaitHandle.WaitOne();
            }
            catch (Exception e) {
                CompleteInitTask(e);
                throw e;
            }
            finally {
                //
                // Report Stopped
                SetStatus(Interop.SERVICE_STOPPED);
            }
        }


        private int SvcCtrlHandler(int command, int eventType, IntPtr eventData, IntPtr eventContext) {
            //
            // Handle service control operation
            switch (command) {
                case Interop.SERVICE_CONTROL_STOP:
                    SetStatus(Interop.SERVICE_STOP_PENDING);

                    //
                    // Signal the service to stop
                    _cancellationToken.Cancel();
                    break;
                default:
                    break;
            }

            return 0;
        }

        private void SetStatus(int state, int error = 0) {
            if (_serviceHandle == IntPtr.Zero) {
                throw new ArgumentNullException(nameof(_serviceHandle));
            }

            var status = new Interop.SERVICE_STATUS() {
                currentState = state,
                win32ExitCode = error,
                serviceType = Interop.SERVICE_TYPE_WIN32_OWN_PROCESS,
                controlsAccepted = (state == Interop.SERVICE_START_PENDING) ? 0 : Interop.SERVICE_ACCEPT_STOP,
                waitHint = (state == Interop.SERVICE_START_PENDING) || (state == Interop.SERVICE_STOP_PENDING) ? PENDING_TIMEOUT : 0
            };

            Interop.SetServiceStatus(_serviceHandle, ref status); // Ignore errors
        }

        private Task CreateInitTask() {
            return new Task(s => {
                var state = (TaskState)s;

                if (state.Exception != null) {
                    state.Exception.Throw();
                }
            }, 
            new TaskState());
        }

        private void CompleteInitTask(Exception e) {
            if (_svcInitTask.Status != TaskStatus.Created) {
                return; // The task has been started already
            }

            //
            // Fail the task
            if (e != null) {
                ((TaskState)_svcInitTask.AsyncState).Exception = ExceptionDispatchInfo.Capture(e);
            }

            _svcInitTask.Start();
        }
    }
}