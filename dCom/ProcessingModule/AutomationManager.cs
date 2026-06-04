using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


		private void AutomationWorker_DoWork()
		{
			const int STEP = 10;

			PointIdentifier l = new PointIdentifier(PointType.ANALOG_INPUT,2300);
            PointIdentifier stop = new PointIdentifier(PointType.DIGITAL_OUTPUT,2700);
            PointIdentifier ventil = new PointIdentifier(PointType.DIGITAL_OUTPUT, 2702);
            PointIdentifier p1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 2705);
            PointIdentifier p2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 2706);
            PointIdentifier n1 = new PointIdentifier(PointType.ANALOG_OUTPUT, 1300);

            List<PointIdentifier> pointList = new List<PointIdentifier> { l, stop, ventil, p1, p2, n1 };
            EGUConverter eguConverter = new EGUConverter();
            double trenutniPolozajKapijeEGU = 0;

			while (!disposedValue) 
			{
				automationTrigger.WaitOne();

                List<IPoint> points = storage.GetPoints(pointList);
                int pomeraj = 0;

				if (points[3].RawValue == 1) 
				{
					pomeraj += STEP;
				}

				if (points[4].RawValue == 1) 
				{
					pomeraj -= STEP;
				}

				trenutniPolozajKapijeEGU = eguConverter.ConvertToEGU(1, 0, points[5].RawValue);

				processingManager.ExecuteWriteCommand(
						points[5].ConfigItem, 
						configuration.GetTransactionId(), 
						configuration.UnitAddress, 1300, 
						(int)(trenutniPolozajKapijeEGU + pomeraj));

                trenutniPolozajKapijeEGU = eguConverter.ConvertToEGU(1, 0, points[5].RawValue);

				if (trenutniPolozajKapijeEGU > points[5].ConfigItem.HighLimit) 
				{
					processingManager.ExecuteWriteCommand(
						points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2705, 0);

				}else if (trenutniPolozajKapijeEGU < points[5].ConfigItem.LowLimit)
				{
					processingManager.ExecuteWriteCommand(
						points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2706, 0);
				}
            }
        }

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
