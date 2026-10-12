using strange.extensions.dispatcher.eventdispatcher.api;

namespace strange.extensions.mediation.api
{
	public interface IEventView
	{
		IEventDispatcher dispatcher { get; set; }
	}
}

