using Com.Udogames.Core.DebugLogger;
using strange.extensions.command.api;
using strange.extensions.command.impl;
using strange.extensions.context.api;
using strange.extensions.dispatcher.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.implicitBind.api;
using strange.extensions.implicitBind.impl;
using strange.extensions.injector.api;
using strange.extensions.injector.impl;
using strange.extensions.mediation.api;
using strange.extensions.mediation.impl;
using strange.extensions.sequencer.api;
using strange.extensions.sequencer.impl;
using strange.framework.api;
using strange.framework.impl;
using UnityEngine;

namespace strange.extensions.context.impl
{
	public class SingleMVCSContext : Context
	{
		private IInjectionBinder _injectionBinder;

		/// A Binder that handles dependency injection binding and instantiation
		public IInjectionBinder injectionBinder
		{
			get { return _injectionBinder; }
			set { _injectionBinder = value; }
		}
		
		/// A Binder that maps Signals to Commands
		public ICommandBinder commandBinder{get;set;}

		/// A Binder that serves as the Event bus for the Context
		public IEventDispatcher dispatcher{get;set;}

		/// A Binder that maps Views to Mediators
		public IMediationBinder mediationBinder{get;set;}

		//Interprets implicit bindings
		public IImplicitBinder implicitBinder { get; set; }

		/// A Binder that maps Events to Sequences
		public ISequencer sequencer{get;set;}


		/// A list of Views Awake before the Context is fully set up.
		protected ISemiBinding viewCache = new SemiBinding();
		
		public SingleMVCSContext() 
		{}

		/// The recommended Constructor
		/// Just pass in the instance of your ContextView. Everything will begin automatically.
		/// Other constructors offer the option of interrupting startup at useful moments.
		public SingleMVCSContext(MonoBehaviour view) : this(view, ContextStartupFlags.AUTOMATIC)
		{
		}

		public SingleMVCSContext(MonoBehaviour view, ContextStartupFlags flags)
		{
			SetContextView(view);
			addCoreComponents();
			this.autoStartup = (flags & ContextStartupFlags.MANUAL_LAUNCH) != ContextStartupFlags.MANUAL_LAUNCH;
			if ((flags & ContextStartupFlags.MANUAL_MAPPING) != ContextStartupFlags.MANUAL_MAPPING)
			{
#if UNITY_EDITOR
				DebugX.Init().Then(() => Start()).Catch(exception =>
				{
					Debug.LogError(exception);
					Start();
				});
#else
				Start();
#endif
			}
		}

		public SingleMVCSContext(MonoBehaviour view, bool autoMapping)
			: this(view, (autoMapping) ? ContextStartupFlags.MANUAL_MAPPING : ContextStartupFlags.MANUAL_LAUNCH | ContextStartupFlags.MANUAL_MAPPING)
		{
		}
		
		override public IContext SetContextView(object view)
		{
			contextView = (view as MonoBehaviour).gameObject;
			if (contextView == null)
			{
				throw new ContextException("MVCSContext requires a ContextView of type MonoBehaviour", ContextExceptionType.NO_CONTEXT_VIEW);
			}
			return this;
		}

		/// Map the relationships between the Binders.
		/// Although you can override this method, it is recommended
		/// that you provide all your application bindings in `mapBindings()`.
		protected override void addCoreComponents()
		{
			base.addCoreComponents();

			injectionBinder = new InjectionBinder();
			
			injectionBinder.Bind<IInstanceProvider>().Bind<IInjectionBinder>().ToValue(injectionBinder);
			injectionBinder.Bind<IContext>().ToValue(this).ToName(ContextKeys.CONTEXT);
			injectionBinder.Bind<ICommandBinder>().To<CommandBinder>().ToSingleton();
			//This binding is for local dispatchers
			injectionBinder.Bind<IEventDispatcher>().To<EventDispatcher>();
			//This binding is for the common system bus
			injectionBinder.Bind<IEventDispatcher>().To<EventDispatcher>().ToSingleton().ToName(ContextKeys.CONTEXT_DISPATCHER).ToName(ContextKeys.CROSS_CONTEXT_DISPATCHER);
			injectionBinder.Bind<IMediationBinder>().To<MediationBinder>().ToSingleton();
			injectionBinder.Bind<ISequencer>().To<Sequencer>().ToSingleton();
			injectionBinder.Bind<IImplicitBinder>().To<ImplicitBinder>().ToSingleton();
		}
		
		protected override void instantiateCoreComponents()
		{
			base.instantiateCoreComponents();
			if (contextView == null)
			{
				throw new ContextException("MVCSContext requires a ContextView of type MonoBehaviour", ContextExceptionType.NO_CONTEXT_VIEW);
			}
			injectionBinder.Bind<GameObject>().ToValue(contextView).ToName(ContextKeys.CONTEXT_VIEW);
			commandBinder = injectionBinder.GetInstance<ICommandBinder>() as ICommandBinder;
			
			dispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER) as IEventDispatcher;
			mediationBinder = injectionBinder.GetInstance<IMediationBinder>() as IMediationBinder;
			sequencer = injectionBinder.GetInstance<ISequencer>() as ISequencer;
			implicitBinder = injectionBinder.GetInstance<IImplicitBinder>() as IImplicitBinder;

			(dispatcher as ITriggerProvider).AddTriggerable(commandBinder as ITriggerable);
			(dispatcher as ITriggerProvider).AddTriggerable(sequencer as ITriggerable);
		}
		
		protected override void postBindings()
		{
			//It's possible for views to fire their Awake before bindings. This catches any early risers and attaches their Mediators.
			mediateViewCache();
			//Ensure that all Views underneath the ContextView are triggered
			mediationBinder.Trigger(MediationEvent.AWAKE, (contextView as GameObject).GetComponent<ContextView>());
		}

		/// Fires ContextEvent.START
		/// Whatever Command/Sequence you want to happen first should 
		/// be mapped to this event.
		public override void Launch()
		{
			dispatcher.Dispatch(ContextEvent.START);
		}
		
		/// Gets an instance of the provided generic type.
		/// Always bear in mind that doing this risks adding
		/// dependencies that must be cleaned up when Contexts
		/// are removed.
		override public object GetComponent<T>()
		{
			return GetComponent<T>(null);
		}

		/// Gets an instance of the provided generic type and name from the InjectionBinder
		/// Always bear in mind that doing this risks adding
		/// dependencies that must be cleaned up when Contexts
		/// are removed.
		override public object GetComponent<T>(object name)
		{
			IInjectionBinding binding = injectionBinder.GetBinding<T>(name);
			if (binding != null)
			{
				return injectionBinder.GetInstance<T>(name);
			}
			return null;
		}
		
		override public void AddView(object view)
		{
			if (mediationBinder != null)
			{
				mediationBinder.Trigger(MediationEvent.AWAKE, view as IView);
			}
			else
			{
				cacheView(view as MonoBehaviour);
			}
		}
		
		override public void RemoveView(object view)
		{
			mediationBinder.Trigger(MediationEvent.DESTROYED, view as IView);
		}

		override public void EnableView(object view)
		{
			if (mediationBinder == null)
				return;
			mediationBinder.Trigger(MediationEvent.ENABLED, view as IView);
		}

		override public void DisableView(object view)
		{
			if (mediationBinder == null)
				return;
			mediationBinder.Trigger(MediationEvent.DISABLED, view as IView);
		}

		/// Caches early-riser Views.
		/// 
		/// If a View is on stage at startup, it's possible for that
		/// View to be Awake before this Context has finished initing.
		/// `cacheView()` maintains a list of such 'early-risers'
		/// until the Context is ready to mediate them.
		virtual protected void cacheView(MonoBehaviour view)
		{
			if (viewCache.constraint.Equals(BindingConstraintType.ONE))
			{
				viewCache.constraint = BindingConstraintType.MANY;
			}
			viewCache.Add(view);
		}

		/// Provide mediation for early-riser Views
		virtual protected void mediateViewCache()
		{
			if (mediationBinder == null)
				throw new ContextException("MVCSContext cannot mediate views without a mediationBinder", ContextExceptionType.NO_MEDIATION_BINDER);
			
			object[] values = viewCache.value as object[];
			if (values == null)
			{
				return;
			}
			int aa = values.Length;
			for (int a = 0; a < aa; a++)
			{
				mediationBinder.Trigger(MediationEvent.AWAKE, values[a] as IView);
			}
			viewCache = new SemiBinding();
		}

		/// Clean up. Called by a ContextView in its OnDestroy method
		public override void OnRemove()
		{
			base.OnRemove();
			commandBinder.OnRemove();
			(injectionBinder as IBinder).OnRemove();
		}
	}
}

