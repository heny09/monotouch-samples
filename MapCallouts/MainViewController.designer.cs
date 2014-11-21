// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;

namespace MapCallouts
{
	[Foundation.Register("MainViewController")]
	partial class MainViewController
	{
		[Foundation.Outlet]
		MapKit.MKMapView mapView { get; set; }

		[Foundation.Action("cityAction:")]
		partial void cityAction (Foundation.NSObject sender);

		[Foundation.Action("bridgeAction:")]
		partial void bridgeAction (Foundation.NSObject sender);

		[Foundation.Action("allAction:")]
		partial void allAction (Foundation.NSObject sender);
	}
}
