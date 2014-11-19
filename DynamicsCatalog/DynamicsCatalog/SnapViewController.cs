using CoreGraphics;
using System;
using UIKit;

namespace DynamicsCatalog {

	public partial class SnapViewController : UIViewController {

		public UIDynamicAnimator Animator { get; private set; }

		public SnapViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			View.AddGestureRecognizer (new UITapGestureRecognizer ((gesture) => {
				Animator = new UIDynamicAnimator (View);
                // TODO: Added "using CoreGraphics" in order for (CGPoint) cast to work
				Animator.AddBehavior (new UISnapBehavior (square, (CGPoint)gesture.LocationInView ((UIView)View)));
			}));
		}
	}
}