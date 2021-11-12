using System;
using System.Threading.Tasks;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Multiplexer
{
	public class MultiplexedPlatformService : IPlatformService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>, IDisposable
	{
		private abstract class Info
		{
			public abstract bool LoggedIn(object o);
			public abstract MultiplexedChannel? GetDefaultChannel(object o);

			public abstract Task Start(object o);
			public abstract Task Stop(object o);

			public abstract object GetEventHost(MultiplexedPlatformService ps);
			public abstract void Subscribe(object o, object eventHost);
			public abstract void Unsubscribe(object o, object eventHost);
		}

		private class Info<TService, TChannel, TMsg> : Info
			where TService : IPlatformService<TService, TChannel, TMsg>
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
		{
			public static readonly Info<TService, TChannel, TMsg> INSTANCE = new();

			public override MultiplexedChannel? GetDefaultChannel(object o)
			{
				var channel = ((TService) o).DefaultChannel;
				return channel is null ? null : MultiplexedChannel.From<TChannel, TMsg>(channel);
			}

			public override bool LoggedIn(object o) => ((TService) o).LoggedIn;

			public override Task Start(object o) => ((TService) o).Start();
			public override Task Stop(object o) => ((TService) o).Stop();

			public override object GetEventHost(MultiplexedPlatformService ps)
				=> new EventHost(ps);

			public override void Subscribe(object o, object eventHost)
			{
				var svc = (TService) o;
				var host = (EventHost) eventHost;
				svc.OnAuthenticatedStateChanged += host.OnAuthenticatedStateChanged;
				svc.OnChatConnected += host.OnChatConnected;
				svc.OnJoinChannel += host.OnJoinChannel;
				svc.OnRoomStateUpdated += host.OnRoomStateUpdated;
				svc.OnLeaveChannel += host.OnLeaveChannel;
				svc.OnTextMessageReceived += host.OnTextMessageReceived;
			}

			public override void Unsubscribe(object o, object eventHost)
			{
				var svc = (TService) o;
				var host = (EventHost) eventHost;
				svc.OnAuthenticatedStateChanged -= host.OnAuthenticatedStateChanged;
				svc.OnChatConnected -= host.OnChatConnected;
				svc.OnJoinChannel -= host.OnJoinChannel;
				svc.OnRoomStateUpdated -= host.OnRoomStateUpdated;
				svc.OnLeaveChannel -= host.OnLeaveChannel;
				svc.OnTextMessageReceived -= host.OnTextMessageReceived;
			}

			private static MultiplexedPlatformService From(TService service)
				=> From<TService, TChannel, TMsg>(service);

			private sealed class EventHost
			{
				// only keep a weak reference so that the MultiplexedPlatformService can be GC'd
				private readonly WeakReference<MultiplexedPlatformService> svc;
				public EventHost(MultiplexedPlatformService svc)
					=> this.svc = new(svc);

				private MultiplexedPlatformService? Get()
					=> svc.TryGetTarget(out var target) ? target : null;

				internal void OnAuthenticatedStateChanged(TService obj)
					=> Get()?.OnAuthenticatedStateChanged?.Invoke(From(obj));
				internal void OnChatConnected(TService obj)
					=> Get()?.OnChatConnected?.Invoke(From(obj));
				internal void OnJoinChannel(TService arg1, TChannel arg2)
					=> Get()?.OnJoinChannel?.Invoke(From(arg1), MultiplexedChannel.From<TChannel, TMsg>(arg2));
				internal void OnRoomStateUpdated(TService arg1, TChannel arg2)
					=> Get()?.OnRoomStateUpdated?.Invoke(From(arg1), MultiplexedChannel.From<TChannel, TMsg>(arg2));
				internal void OnLeaveChannel(TService arg1, TChannel arg2)
					=> Get()?.OnLeaveChannel?.Invoke(From(arg1), MultiplexedChannel.From<TChannel, TMsg>(arg2));
				internal void OnTextMessageReceived(TService arg1, TMsg arg2)
					=> Get()?.OnTextMessageReceived?.Invoke(From(arg1), MultiplexedMessage.From<TMsg, TChannel>(arg2));
			}
		}

		public event Action<MultiplexedPlatformService>? OnAuthenticatedStateChanged;
		public event Action<MultiplexedPlatformService>? OnChatConnected;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnJoinChannel;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnRoomStateUpdated;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnLeaveChannel;
		public event Action<MultiplexedPlatformService, MultiplexedMessage>? OnTextMessageReceived;

		private readonly Info info;
		private readonly object service;
		private readonly object eventHost;
		private bool disposedValue;

		public object Underlying => service;

		private MultiplexedPlatformService(object service, Info info)
		{
			(this.info, this.service) = (info, service);
			eventHost = info.GetEventHost(this);
			info.Subscribe(service, eventHost);
		}

		public static MultiplexedPlatformService From<TService, TChannel, TMsg>(TService service)
			where TService : IPlatformService<TService, TChannel, TMsg>
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(service, Info<TService, TChannel, TMsg>.INSTANCE);

		public bool LoggedIn => info.LoggedIn(service);

		public MultiplexedChannel? DefaultChannel => info.GetDefaultChannel(service);

		Task IPlatformService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>.Start()
			=> info.Start(service);

		Task IPlatformService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>.Stop()
			=> info.Stop(service);

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}

				info.Unsubscribe(service, eventHost);

				disposedValue = true;
			}
		}

		// we need this so that we can auto-unsubscribe from events
		~MultiplexedPlatformService()
		{
		    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		    Dispose(disposing: false);
		}

		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
