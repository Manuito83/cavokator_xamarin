using System;
using System.Collections.Generic;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    
    internal class NotamFieldsAdapter : RecyclerView.Adapter
    {
        public event EventHandler<MapEventArgs> MapClicked;
        public event EventHandler<ShareEventArgs> ShareClicked;

        private List<object> mRecyclerNotamList;

        public override int ItemCount => mRecyclerNotamList.Count;
        
        public NotamFieldsAdapter(List<object> recyclerNotamList)
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

            if (mRecyclerNotamList[position] is MyErrorRecycler)
            {
                return 2;
            }

            if (mRecyclerNotamList[position] is MyCategoryRecycler)
            {
                return 3;
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
                    vh = new NotamViewHolder(v2, MapClick, ShareClick);
                    break;

                case 2:
                    View v3 = inflater.Inflate(Resource.Layout.notam_error_card, parent, false);
                    vh = new ErrorViewHolder(v3);
                    break;

                case 3:
                    View v4 = inflater.Inflate(Resource.Layout.notam_category_card, parent, false);
                    vh = new CategoryViewHolder(v4);
                    break;

                default:
                    View vDef = inflater.Inflate(Resource.Layout.notam_error_card, parent, false);
                    vh = new ErrorViewHolder(vDef);
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

                    // TOPLAYOUT: ID, MAP, SHARE
                    if (notamCard.DisableTopLayout)
                    {
                        vh2.NotamCardMainLayout.RemoveView(vh2.NotamCardTopLayout);
                    }
                    else
                    {
                        vh2.NotamIdTextView.TextFormatted = notamCard.NotamId;
                        vh2.NotamIdTextView.MovementMethod = new LinkMovementMethod();

                        vh2.NotamShareImage.SetImageResource(Resource.Drawable.ic_share_variant_black_48dp);
                        vh2.NotamShareString = notamCard.NotamShareString;
                        vh2.NotamSharedAirportIcao = notamCard.NotamSharedAirportIcao;

                        if (notamCard.NotamMapLatitude == 9999)
                        {
                            vh2.NotamCardMapShareLayout.RemoveView(vh2.NotamMapImageView);
                        }
                        else
                        {
                            vh2.NotamMapImageView.SetImageResource(Resource.Drawable.ic_world_map);
                            vh2.NotamMapLatitude = notamCard.NotamMapLatitude;
                            vh2.NotamMapLongitude = notamCard.NotamMapLongitude;
                            vh2.NotamMapRadius = notamCard.NotamMapRadius;
                        }

                    }

                    // SUBCATEGORIES
                    if (!notamCard.DisableCategories)
                    {
                        vh2.NotamMainSubcategoryTextView.Text = notamCard.NotamMainSubcategory;

                        if (!notamCard.DisableSecondarySubcategory)
                        {
                            vh2.NotamSecondarySubcategoryArrowImageView.SetImageResource(Resource.Drawable.ic_menu_right_black_48dp);
                            vh2.NotamSecondarySubcategoryTextView.Text = notamCard.NotamSecondarySubcategory;
                        }
                        else
                        {
                            vh2.NotamSecondarySubcategoryLayout.RemoveView(vh2.NotamSecondarySubcategoryTextView);
                            vh2.NotamSecondarySubcategoryLayout.RemoveView(vh2.NotamSecondarySubcategoryArrowImageView);
                            vh2.NotamSubcategoriesLayout.RemoveView(vh2.NotamSecondarySubcategoryLayout);
                        }
                    }
                    else
                    {
                        vh2.NotamCardMainLayout.RemoveView(vh2.NotamSubcategoriesLayout);
                    }

                    // MAIN NOTAM TEXT
                    vh2.NotamFreeTextTextView.Text = notamCard.NotamFreeText;

                    // TIME FROM TO
                    if (notamCard.NotamTimeIsActive)
                    {
                        vh2.NotamTimeFromToCalendarImageView.SetImageResource(Resource.Drawable.ic_calendar_multiple_red_48dp);
                    }
                    else
                    {
                        vh2.NotamTimeFromToCalendarImageView.SetImageResource(Resource.Drawable.ic_calendar_multiple_black_48dp);
                    }
                        
                    vh2.NotamTimeFromTextView.Text = notamCard.NotamTimeFrom;

                    vh2.NotamTimeFromToArrowImageView.SetImageResource(Resource.Drawable.ic_menu_right_black_48dp);

                    vh2.NotamTimeToTextView.Text = notamCard.NotamTimeTo;
                    

                    break;

                case 2:
                    ErrorViewHolder vh3 = (ErrorViewHolder)holder;
                    MyErrorRecycler errorCard = (MyErrorRecycler)mRecyclerNotamList[position];
                    vh3.ErrorTextView.Text = errorCard.ErrorString;
                    break;

                case 3:
                    CategoryViewHolder vh4 = (CategoryViewHolder)holder;
                    MyCategoryRecycler categoryCard = (MyCategoryRecycler)mRecyclerNotamList[position];
                    vh4.CategoryTextView.Text = categoryCard.CategoryString;
                    break;
            }
        }

        private void MapClick(string id, float latitude, float longitude, int radius)
        {
            MapClicked?.Invoke(this, new MapEventArgs(id, latitude, longitude, radius));
        }

        private void ShareClick(string id, string raw, View view)
        {
            ShareClicked?.Invoke(this, new ShareEventArgs(id, raw, view));
        }
    }

    internal class AirportViewHolder : RecyclerView.ViewHolder
    {
        public TextView AirportNameTextView { get; }

        public AirportViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            AirportNameTextView = itemView.FindViewById<TextView>(Resource.Id.airportCard_name);
            AirportNameTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
        }
    }

    internal class NotamViewHolder : RecyclerView.ViewHolder
    {
        public CardView NotamMainCardView { get; }
        public LinearLayout NotamCardMainLayout { get; }
        public RelativeLayout NotamCardTopLayout { get; }
        public LinearLayout NotamCardMapShareLayout { get; }

        public TextView NotamIdTextView { get; }

        public ImageView NotamMapImageView { get; }
        public float NotamMapLatitude { get; set; }
        public float NotamMapLongitude { get; set; }
        public int NotamMapRadius { get; set; }

        public ImageView NotamShareImage { get; }
        public string NotamShareString { get; set; }
        public string NotamSharedAirportIcao { get; set; }

        public LinearLayout NotamSubcategoriesLayout { get; }
        public TextView NotamMainSubcategoryTextView { get; }
        public RelativeLayout NotamSecondarySubcategoryLayout { get; }
        public TextView NotamSecondarySubcategoryTextView { get; }
        public ImageView NotamSecondarySubcategoryArrowImageView { get; }

        public TextView NotamFreeTextTextView { get; }

        public ImageView NotamTimeFromToCalendarImageView { get; }
        public TextView NotamTimeFromTextView { get; }
        public ImageView NotamTimeFromToArrowImageView { get; }
        public TextView NotamTimeToTextView { get; }

        public NotamViewHolder(View itemView, Action<string, float, float, int> mapListener,
            Action<string, string, View> shareListener) : base(itemView)
        {
            // Locate and cache view references:
            NotamMainCardView = itemView.FindViewById<CardView>(Resource.Id.notamCard_MainCard);
            NotamMainCardView.SetCardBackgroundColor(new ApplyTheme().GetColor(DesiredColor.CardViews));

            // Main layouts
            NotamCardMainLayout = itemView.FindViewById<LinearLayout>(Resource.Id.notamCard_MainLayout);
            NotamCardTopLayout = itemView.FindViewById<RelativeLayout>(Resource.Id.notamCard_TopLayout);
            NotamCardMapShareLayout = itemView.FindViewById<LinearLayout>(Resource.Id.notamCard_MapShareLayout);

            // Childs in TopLayout
            NotamIdTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_Id);
            NotamIdTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            NotamMapImageView = itemView.FindViewById<ImageView>(Resource.Id.notamCard_Map);
            NotamMapImageView.Click += (sender, e) => mapListener(NotamIdTextView.Text, NotamMapLatitude, NotamMapLongitude, NotamMapRadius);

            NotamShareImage = itemView.FindViewById<ImageView>(Resource.Id.notamCard_Share);
            NotamShareImage.Click += (sender, e) => shareListener(NotamSharedAirportIcao, NotamShareString, itemView);

            // Subcategories
            NotamSubcategoriesLayout = itemView.FindViewById<LinearLayout>(Resource.Id.notamCard_SubcategoriesLayout);

            NotamMainSubcategoryTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_MainSubcategory);
            NotamMainSubcategoryTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
            NotamMainSubcategoryTextView.SetTextSize(ComplexUnitType.Dip, 10);

            NotamSecondarySubcategoryLayout = itemView.FindViewById<RelativeLayout>(Resource.Id.notamCard_SecondarySubcategoryLayout);

            NotamSecondarySubcategoryArrowImageView = itemView.FindViewById<ImageView>(Resource.Id.notamCard_subcategoryArrow);
            
            NotamSecondarySubcategoryTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_SecondarySubcategory);
            NotamSecondarySubcategoryTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
            NotamSecondarySubcategoryTextView.SetTextSize(ComplexUnitType.Dip, 10);

            // Free Notam Text
            NotamFreeTextTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_FreeText);
            NotamFreeTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            NotamFreeTextTextView.SetTextSize(ComplexUnitType.Dip, 12);

            // From To Times
            NotamTimeFromToCalendarImageView = itemView.FindViewById<ImageView>(Resource.Id.notamCard_TimeFromToCalendar);

            NotamTimeFromTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_TimeFromText);
            NotamTimeFromTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            NotamTimeFromTextView.SetTextSize(ComplexUnitType.Dip, 11);

            NotamTimeFromToArrowImageView = itemView.FindViewById<ImageView>(Resource.Id.notamCard_TimeFromToArrow);

            NotamTimeToTextView = itemView.FindViewById<TextView>(Resource.Id.notamCard_TimeToText);
            NotamTimeToTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            NotamTimeToTextView.SetTextSize(ComplexUnitType.Dip, 11);

        }

    }

    internal class ErrorViewHolder : RecyclerView.ViewHolder
    {
        public TextView ErrorTextView { get; }
        public CardView ErrorCardView { get;  }

        public ErrorViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            ErrorTextView = itemView.FindViewById<TextView>(Resource.Id.notam_ErrorTextView);
            ErrorTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));

            ErrorCardView = itemView.FindViewById<CardView>(Resource.Id.notam_ErrorMainCard);
            ErrorCardView.SetCardBackgroundColor(new ApplyTheme().GetColor(DesiredColor.LightYellowBackground));
        }
    }

    internal class CategoryViewHolder : RecyclerView.ViewHolder
    {
        public TextView CategoryTextView { get; }

        public CategoryViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            CategoryTextView = itemView.FindViewById<TextView>(Resource.Id.notam_CategoryTextView);

            CategoryTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));

            GradientDrawable categoryTitleBackground = new GradientDrawable();
            categoryTitleBackground.SetCornerRadius(8);
            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
            categoryTitleBackground.SetStroke(3, Color.Black);
            CategoryTextView.Background = categoryTitleBackground;
        }
    }


    internal class MyAirportRecycler
    {
        public string Name;
    }


    internal class MyNotamCardRecycler
    {
        public bool DisableTopLayout;

        public SpannableString NotamId;

        public float NotamMapLatitude;
        public float NotamMapLongitude;
        public int NotamMapRadius;

        public string NotamShareString;
        public string NotamSharedAirportIcao;

        public bool DisableCategories;
        public bool DisableSecondarySubcategory;
        public string NotamMainSubcategory;
        public string NotamSecondarySubcategory;

        public string NotamFreeText;

        public string NotamTimeFrom;
        public string NotamTimeTo;
        public bool NotamTimeIsActive;
    }


    internal class MyErrorRecycler
    {
        public string ErrorString;
    }


    internal class MyCategoryRecycler
    {
        public string CategoryString;
    }


    internal class MapEventArgs
    {
        public string Id { get; }
        public float Latitude { get; }
        public float Longitude { get; }
        public int Radius { get; }

        public MapEventArgs(string id, float latitude, float longitude, int radius)
        {
            Id = id;
            Latitude = latitude;
            Longitude = longitude;
            Radius = radius;
        }
    }

    internal class ShareEventArgs
    {
        public string Id { get; }
        public string RawNotam { get; }
        public View MyViewToShare { get; }

        public ShareEventArgs(string id, string rawNotam, View myView)
        {
            Id = id;
            RawNotam = rawNotam;
            MyViewToShare = myView;
        }
    }
}