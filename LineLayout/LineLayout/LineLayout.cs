using System;
using CoreGraphics;
using Foundation;
using UIKit;
using CoreGraphics;
using CoreAnimation;

namespace LineLayout
{
	public partial class LineLayout : UICollectionViewFlowLayout
	{
		public const float ITEM_SIZE = 200.0f;
		public const int ACTIVE_DISTANCE = 200;
		public const float ZOOM_FACTOR = 0.3f;
		
		public LineLayout ()
		{	
			ItemSize = new CGSize (ITEM_SIZE, ITEM_SIZE);
			ScrollDirection = UICollectionViewScrollDirection.Horizontal;
			SectionInset = new UIEdgeInsets (200, 0.0f, 200, 0.0f);
			MinimumLineSpacing = 50.0f;		
		}

		public override bool ShouldInvalidateLayoutForBoundsChange (CGRect newBounds)
		{
			return (bool )true;
		}		
		
		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect (CGRect rect)
		{
			var array = (UICollectionViewLayoutAttributes[])base.LayoutAttributesForElementsInRect ((CGRect)rect);
			var visibleRect = new CGRect (CollectionView.ContentOffset, CollectionView.Bounds.Size);
						
			foreach (var attributes in array) {
				if (attributes.Frame.IntersectsWith ((CGRect)rect)) {
                    // TODO: Cast nfloat to float
					float distance = (float)(visibleRect.GetMidX () - attributes.Center.X);
					float normalizedDistance = distance / ACTIVE_DISTANCE;
					if (Math.Abs (distance) < ACTIVE_DISTANCE) {
						float zoom = 1 + ZOOM_FACTOR * (1 - Math.Abs (normalizedDistance));
						attributes.Transform3D = (CATransform3D)CATransform3D.MakeScale ((nfloat)zoom, (nfloat)zoom, (nfloat)1.0f);
						attributes.ZIndex = 1;											
					}
				}
			}
			return (UICollectionViewLayoutAttributes[])array;
		}
		
		public override CGPoint TargetContentOffset (CGPoint proposedContentOffset, CGPoint scrollingVelocity)
		{
			float offSetAdjustment = float.MaxValue;
            // TODO: Removed CGPoint cast from CGPoint.X
			float horizontalCenter = (float)((proposedContentOffset.X + (this.CollectionView.Bounds.Size.Width / 2.0)));
            // TODO: Removed CGPoint cast from CGRect constructor that takes nfloats
			CGRect targetRect = new CGRect (proposedContentOffset.X, 0.0f, this.CollectionView.Bounds.Size.Width, this.CollectionView.Bounds.Size.Height);
			var array = (UICollectionViewLayoutAttributes[])base.LayoutAttributesForElementsInRect ((CGRect)targetRect);
			foreach (var layoutAttributes in array) {
                // TODO: Cast nfloat to float
				float itemHorizontalCenter = (float)layoutAttributes.Center.X;
				if (Math.Abs (itemHorizontalCenter - horizontalCenter) < Math.Abs (offSetAdjustment)) {
					offSetAdjustment = itemHorizontalCenter - horizontalCenter;
				}
			}
            // TODO: Removed (CGPoint) cast from CGPoint.X and from new CGPoint
			return new CGPoint (proposedContentOffset.X + offSetAdjustment, proposedContentOffset.Y);
		}
	}
}