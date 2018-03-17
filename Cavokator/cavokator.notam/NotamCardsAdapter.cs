using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    internal class NotamCardsAdapter : RecyclerView.Adapter
    {
        private List<object> mRecyclerNotamList;

        public override int ItemCount => mRecyclerNotamList.Count;

        public NotamCardsAdapter(List<object> recyclerNotamList)
        {
            mRecyclerNotamList = recyclerNotamList;
        }

        public override int GetItemViewType(int position)
        {
            if (mRecyclerNotamList[position] is MyAirportRecycler)
            {
                return 0;
            }

            if (mRecyclerNotamList[position] is MyNotamCardRecycler)
            {
                return 1;
            }

            return -1;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            RecyclerView.ViewHolder vh;
            LayoutInflater inflater = LayoutInflater.From(parent.Context);

            switch (viewType)
            {
                case 0:
                    View v1 = inflater.Inflate(Resource.Layout.notam_airports, parent, false);
                    vh = new AirportViewHolder(v1);
                    break;

                case 1:
                    View v2 = inflater.Inflate(Resource.Layout.notam_cards, parent, false);
                    vh = new NotamViewHolder(v2);
                    break;
                default:
                    View v3 = inflater.Inflate(Resource.Layout.notam_cards, parent, false);
                    vh = new NotamViewHolder(v3);
                    break;
            }

            return vh;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            switch (holder.ItemViewType)
            {
                case 0:
                    AirportViewHolder vh1 = (AirportViewHolder)holder;
                    MyAirportRecycler airport = (MyAirportRecycler)mRecyclerNotamList[position];
                    vh1.AirportNameTextView.Text = airport.Name;
                    break;

                case 1:
                    NotamViewHolder vh2 = (NotamViewHolder)holder;
                    MyNotamCardRecycler notamCard = (MyNotamCardRecycler)mRecyclerNotamList[position];
                    vh2.NotamIdTextView.TextFormatted = notamCard.NotamId;
                    vh2.NotamIdTextView.MovementMethod = new LinkMovementMethod();
                    vh2.NotamFreeTextTextView.Text = notamCard.NotamFreeText;
                    break;
            }

        }
        
    }

    internal class AirportViewHolder : RecyclerView.ViewHolder
    {
        public TextView AirportNameTextView { get; private set; }

        private string title;

        public AirportViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            AirportNameTextView = itemView.FindViewById<TextView>(Resource.Id.airport_id_AA);
        }

        public void SetTitle(string title)
        {
            this.title = title;
        }
    }

    internal class NotamViewHolder : RecyclerView.ViewHolder
    {
        public TextView NotamIdTextView;
        public TextView NotamFreeTextTextView;

        public NotamViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            NotamIdTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_Id);

            NotamFreeTextTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_FreeText);
            NotamFreeTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            NotamFreeTextTextView.SetTextSize(ComplexUnitType.Dip, 12);
        }
    }

    internal class MyAirportRecycler
    {
        public string Name;
    }

    internal class MyNotamCardRecycler
    {
        public SpannableString NotamId;
        public string NotamFreeText;
    }
}