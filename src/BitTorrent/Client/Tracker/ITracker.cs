using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Client.Tracker;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Client.Tracker
{
    interface ITracker
    {
        event EventHandler BeforeAnnounce;
        event EventHandler<AnnounceResponseEventArgs> AnnounceComplete;
        event EventHandler BeforeScrape;
        event EventHandler<ScrapeResponseEventArgs> ScrapeComplete;

        bool CanAnnounce { get; }
        bool CanScrape { get; }
        int Complete { get; }
        int Downloaded { get;}
        string FailureMessage { get; }
        int Incomplete { get; }
        TimeSpan MinUpdateInterval { get; }
        TrackerState Status { get; }
        TimeSpan UpdateInterval { get; }
        Uri Uri { get; }
        string WarningMessage { get; }

        void Announce(AnnounceParameters parameters, object state);
        void Scrape(ScrapeParameters parameters, object state);
    }
}
