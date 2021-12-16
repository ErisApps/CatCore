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
				svc.OnMessageDeleted += host.OnMessageDeleted;
				svc.OnChatCleared += host.OnChatCleared;
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
				svc.OnMessageDeleted -= host.OnMessageDeleted;
				svc.OnChatCleared -= host.OnChatCleared;
			}

			private static MultiplexedPlatformService From(TService service)
				=> From<TService, TChannel, TMsg>(service);

			private sealed class EventHost
			{
				// only keep a weak reference so that the MultiplexedPlatformService can be GC'd
				private readonly WeakReference<MultiplexedPlatformService> _svc;
				public EventHost(MultiplexedPlatformService svc)
					=> _svc = new(svc);

				private MultiplexedPlatformService? Get()
					=> _svc.TryGetTarget(out var target) ? target : null;

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
				internal void OnMessageDeleted(TService arg1, TChannel arg2, string arg3)
					=> Get()?.OnMessageDeleted?.Invoke(From(arg1), MultiplexedChannel.From<TChannel, TMsg>(arg2), arg3);
				internal void OnChatCleared(TService arg1, TChannel arg2, string? arg3)
					=> Get()?.OnChatCleared?.Invoke(From(arg1), MultiplexedChannel.From<TChannel, TMsg>(arg2), arg3);
			}
		}

		public event Action<MultiplexedPlatformService>? OnAuthenticatedStateChanged;
		public event Action<MultiplexedPlatformService>? OnChatConnected;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnJoinChannel;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnRoomStateUpdated;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnLeaveChannel;
		public event Action<MultiplexedPlatformService, MultiplexedMessage>? OnTextMessageReceived;
		public event Action<MultiplexedPlatformService, MultiplexedChannel, string>? OnMessageDeleted;
		public event Action<MultiplexedPlatformService, MultiplexedChannel, string?>? OnChatCleared;

		private readonly Info _info;
		private readonly object _service;
		private readonly object _eventHost;
		private bool _disposedValue;

		public object Underlying => _service;

		private MultiplexedPlatformService(object service, Info info)
		{
			(_info, _service) = (info, service);
			_eventHost = info.GetEventHost(this);
			info.Subscribe(service, _eventHost);
		}

		public static MultiplexedPlatformService From<TService, TChannel, TMsg>(TService service)
			where TService : IPlatformService<TService, TChannel, TMsg>
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(service, Info<TService, TChannel, TMsg>.INSTANCE);

		public bool LoggedIn => _info.LoggedIn(_service);

		public MultiplexedChannel? DefaultChannel => _info.GetDefaultChannel(_service);

		Task IPlatformService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>.Start()
			=> _info.Start(_service);

		Task IPlatformService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>.Stop()
			=> _info.Stop(_service);

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_info.Unsubscribe(_service, _eventHost);

				_disposedValue = true;
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
