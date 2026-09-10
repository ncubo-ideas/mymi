namespace Gnothi.Movil;

// WHEN TO ASK THE ORIGINS AGAIN (NotasGnothiHost/44 §7, step 2b). The review at launch always runs.
// The ones on coming back to the front -or on closing a module, when the home is on screen again-
// run only if the last one FINISHED more than `every` ago, and never two at once. That is the whole
// contract: however often she switches apps, an institution hears from this phone at most once an
// hour per module, and the card is fresh when she comes back after a while.
//
// Pure over the clock it is given, so the suite can turn the hands instead of waiting an hour.
public sealed class ReviewSchedule(TimeSpan every)
{
	private int running;                 // 1 while a review is in flight
	private DateTimeOffset? finished;    // when the last one ended; null before the first

	public TimeSpan Every => every;

	// Whether the first review -the one at launch- has at least begun. Before that, a lifecycle
	// hook has nothing to repeat: the launch sequence owns the first one.
	public bool Started => Volatile.Read(ref running) == 1 || finished is not null;

	// Whether a review would start now: none in flight, and the last one long enough ago.
	public bool IsDue(DateTimeOffset now)
		=> Volatile.Read(ref running) == 0 && (finished is not { } f || now - f >= every);

	// Claim the slot. False when one is already running or the last one is too recent; the caller
	// that gets true owes an End().
	public bool TryBegin(DateTimeOffset now)
	{
		if (finished is { } f && now - f < every) return false;
		return Interlocked.CompareExchange(ref running, 1, 0) == 0;
	}

	public void End(DateTimeOffset now)
	{
		finished = now;
		Volatile.Write(ref running, 0);
	}

	// How long until the next one may run; zero when it may run now.
	public TimeSpan Until(DateTimeOffset now)
		=> finished is { } f && now - f < every ? every - (now - f) : TimeSpan.Zero;
}
