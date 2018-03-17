using System;
using Android.Support.V4.Widget;

namespace Cavokator
{
    internal sealed class NestedScrollViewListener : Java.Lang.Object, NestedScrollView.IOnScrollChangeListener
    {
        public EventHandler<NestedScrollViewListenerEventArgs> OnScrollEvent;
        
        public void OnScrollChange(NestedScrollView v, int scrollX, int scrollY, int oldScrollX, int oldScrollY)
        {
            ScrollEvent(scrollY);
        }

        private void ScrollEvent(int y)
        {
            OnScrollEvent?.Invoke(this, new NestedScrollViewListenerEventArgs(y));
        }
    }

    internal class NestedScrollViewListenerEventArgs
    {
        public int PositionY { get; }

        public NestedScrollViewListenerEventArgs(int y)
        {
            PositionY = y;
        }
    }
}