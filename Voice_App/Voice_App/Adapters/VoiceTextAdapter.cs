using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Voice_App.Adapters
{
    public class VoiceTextAdapter : RecyclerView.Adapter
    {
        List<VoiceString> speechStrings;
        const int LEFT_ITEM = 1;
        const int RIGHT_ITEM = 2;
        Activity context;
        public VoiceTextAdapter(Activity context,List<VoiceString> speechStrings)
        {
            this.context = context;
            this.speechStrings = speechStrings;
        }
        public override int GetItemViewType(int position)
        {
            if(speechStrings[position].IsFromUser)
            {
                return RIGHT_ITEM;
            }
            else
            {
                return LEFT_ITEM;
            }
        }
        public override int ItemCount => speechStrings?.Count ?? 0;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if(holder.ItemViewType == RIGHT_ITEM)
            {
                var rightSideViewHolder = (RightSideViewHolder)holder;
                rightSideViewHolder.RightTextView.Text = speechStrings[position].VoiceLabel;
            }
            else
            {
                var leftSideViewHolder = (LeftSideViewHolder)holder;
                leftSideViewHolder.LeftTextView.Text = speechStrings[position].VoiceLabel;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if(viewType == RIGHT_ITEM)
            {
                var rightCellView = context.LayoutInflater.Inflate(Resource.Layout.listitem_rightcellview, parent, false);
                return new RightSideViewHolder(rightCellView);
            }
            else
            {
                var leftCellView = context.LayoutInflater.Inflate(Resource.Layout.listitem_leftcellview, parent, false);
                return new LeftSideViewHolder(leftCellView);
            }
        }

        class RightSideViewHolder:RecyclerView.ViewHolder
        {
            public TextView RightTextView;
            public RightSideViewHolder(View view):base(view)
            {
                RightTextView = view.FindViewById<TextView>(Resource.Id.rightTextView);
            }
        }
        class LeftSideViewHolder : RecyclerView.ViewHolder
        {
            public TextView LeftTextView;
            public LeftSideViewHolder(View view):base(view)
            {
                LeftTextView = view.FindViewById<TextView>(Resource.Id.leftTextView);
            }
        }


    }
}