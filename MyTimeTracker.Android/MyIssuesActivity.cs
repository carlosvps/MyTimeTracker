﻿using Android.App;
using Android.Widget;
using Android.OS;
using MyTimeTracker.Core.Model;
using MyTimeTracker.Android.Adapter;
using System.Timers;
using System.Collections.Generic;
using System;
using Android.Views;
using Service = MyTimeTracker.Core.Service.Service;
using MyTimeTracker.Android.Provider;

namespace MyTimeTracker.Android
{
    [Activity(Label = "@string/ApplicationName", Icon = "@drawable/Icon")]
    public class MyIssuesActivity : Activity
    {
        private Timer _timer;

        private Worklog _currentWorklog;
        private IList<Issue> _associatedIssueList;
        private IList<Worklog> _worklogList;
        private int _position;

        private MyIssueAdapter myIssueAdapter;

        private Service _service;

        #region Components
        private ListView _myIssuesListView;
        private TextView _issueTrakingTextView;
        private ImageButton _stopTrackingImageButton;
        private TableLayout _issueTrackingTableLayout;
        #endregion

        public MyIssuesActivity()
        {
            _worklogList = new List<Worklog>();
            _timer = new Timer(1000);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.MyIssuesView);


            _service = new Service(new SecuredDataProvider(this.BaseContext));

            _associatedIssueList = _service.GetAssociatedIssues();

            InitializeComponents();
            EventHandlers();

            myIssueAdapter = new MyIssueAdapter(this, _associatedIssueList);
            _myIssuesListView.Adapter = myIssueAdapter;
            _myIssuesListView.EmptyView = FindViewById(Resource.Id.EmptyIssueListTextView);
            _myIssuesListView.FastScrollEnabled = true;
        }

        private void InitializeComponents()
        {
            _myIssuesListView = FindViewById<ListView>(Resource.Id.MyIssuesListView);
            _issueTrakingTextView = FindViewById<TextView>(Resource.Id.IssueTrackingTextView);
            _stopTrackingImageButton = FindViewById<ImageButton>(Resource.Id.StopTrackingImageButton);
            _issueTrackingTableLayout = FindViewById<TableLayout>(Resource.Id.IssueTrackingTableLayout);
        }

        private void EventHandlers()
        {
            _myIssuesListView.ItemClick += StartTracking;
            _stopTrackingImageButton.Click += StopTracking;
        }

        private void StopTracking()
        {
            if (_currentWorklog == null)
            {
                return;
            }

            var timeSpent = (int)DateTime.Now.Subtract(_currentWorklog.Started).TotalSeconds;
            _currentWorklog.TimeSpentInSeconds = timeSpent < 60 ? 60 : _currentWorklog.TimeSpentInSeconds;

            //TODO: Function to Save in a offline case
            _service.SaveWorklog(_currentWorklog);

            var totalTime = int.Parse(_associatedIssueList[_position].fields.timespent.ToString());
            _associatedIssueList[_position].fields.timespent = totalTime + _currentWorklog.TimeSpentInSeconds;
            // _myIssuesListView.Adapter = new MyIssueAdapter(this, _associatedIssueList);
            myIssueAdapter.NotifyDataSetChanged();

            _currentWorklog = null;
            _issueTrakingTextView.Text = GetString(Resource.String.SelectIssueToTrackMessage);

            Toast.MakeText(this, GetString(Resource.String.WorklogSavedMessage), ToastLength.Short).Show();
        }

        private void StopTracking(object sender, EventArgs e)
        {
            _timer.Stop();
            _issueTrackingTableLayout.Visibility = ViewStates.Gone;

            StopTracking();
        }

        private void StartTracking(object sender, AdapterView.ItemClickEventArgs e)
        {
            _issueTrackingTableLayout.Visibility = ViewStates.Visible;
            _position = e.Position;

            var selectedIssue = _associatedIssueList[_position];

            if (_currentWorklog != null)
            {
                if (_currentWorklog.IssueId == selectedIssue.id)
                {
                    return;
                }

                if (_currentWorklog.IssueId != selectedIssue.id)
                {
                    StopTracking();
                }
            }

            _currentWorklog = new Worklog();
            _currentWorklog.IssueId = selectedIssue.id;
            _currentWorklog.Started = DateTime.Now;

            _timer.Start();
            _timer.Elapsed += UpdateStatus;

            Toast.MakeText(this, GetString(Resource.String.WorklogTrackingStartedMessage), ToastLength.Short).Show();
        }

        private void UpdateStatus(object sender, ElapsedEventArgs e)
        {
            this.RunOnUiThread(() =>
                    _issueTrakingTextView.Text =
                                string.Format(GetString(Resource.String.IssueTrackingMessage),
                                                _associatedIssueList[_position].key,
                                                GetFormmatedTimeSpent())
                                );
        }

        private string GetFormmatedTimeSpent()
        {
            return TimeSpan.FromSeconds(DateTime.Now.Subtract(_currentWorklog.Started).TotalSeconds)
                                    .ToString(@"dd\:hh\:mm\:ss");
        }
    }
}

