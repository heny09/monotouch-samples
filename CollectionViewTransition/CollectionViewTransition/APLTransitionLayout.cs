using CoreGraphics;
using Foundation;
using System;
using UIKit;

namespace CollectionViewTransition {

	public class APLTransitionLayout : UICollectionViewTransitionLayout {

		public APLTransitionLayout(UICollectionViewLayout currentLayout, UICollectionViewLayout newLayout) :
			base (currentLayout, newLayout)
		{
		}

		public UIOffset Offset { get; private set; }

		public void SetTransitionProgress (float transitionProgress)
		{
			base.TransitionProgress = transitionProgress;
			float offsetH = (float)GetValueForAnimatedKey ("offsetH");
			float offsetV = (float)GetValueForAnimatedKey ("offsetV");
			Offset = new UIOffset (offsetH, offsetV);
		}

		public void SetOffset (UIOffset offset)
		{
            // TODO: Added "using System;" in order for migration tool nfloat cast to work
			UpdateValue ((nfloat)offset.Horizontal, "offsetH");
			UpdateValue ((nfloat)offset.Vertical, "offsetV");
			Offset = offset;
		}

		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect (CGRect rect)
		{
			UICollectionViewLayoutAttributes[] attributes = (UICollectionViewLayoutAttributes[])base.LayoutAttributesForElementsInRect ((CGRect)rect);
			foreach (var attribute in attributes) {
				CGPoint center = attribute.Center;
				attribute.Center = new CGPoint (center.X + Offset.Horizontal, center.Y + Offset.Vertical);
			}
			return (UICollectionViewLayoutAttributes[])attributes;
		}

		public override UICollectionViewLayoutAttributes LayoutAttributesForItem (NSIndexPath indexPath)
		{
			UICollectionViewLayoutAttributes attributes = base.LayoutAttributesForItem (indexPath);
			CGPoint center = attributes.Center;
			attributes.Center = new CGPoint (center.X + Offset.Horizontal, center.Y + Offset.Vertical);
			return attributes;
		}
	}
}