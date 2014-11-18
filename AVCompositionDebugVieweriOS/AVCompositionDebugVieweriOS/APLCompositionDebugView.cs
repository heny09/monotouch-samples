// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using CoreGraphics;
using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace AVCompositionDebugVieweriOS
{
	public partial class APLCompositionDebugView : UIView
	{
		private const int GapAfterRows = 4;
		private const int IdealRowHeight = 40;
		private const int BannerHeight = 20;
		private const int RightInsetToMatchTimeSlider = 60;
		private const int LeftInsetToMatchTimeSlider = 45;
		private const int LeftMarginInset = 4;
		private CALayer drawingLayer;
		private CMTime duration;
		private float compositionRectWidth;
		private float scaledDurationToWidth;
		private List<List<APLCompositionTrackSegmentInfo>> compositionTracks;
		private List<List<CGPoint>> audioMixTracks;
		private List<APLVideoCompositionStageInfo> videoCompositionStages;

		public AVPlayer Player { get; set; }

		public APLCompositionDebugView (IntPtr handle) : base (handle)
		{
			drawingLayer = Layer;
		}

		public void SynchronizeToCompositoin (AVMutableComposition composition, AVMutableVideoComposition videoComposition, AVMutableAudioMix audioMix)
		{
			compositionTracks = null;
			audioMixTracks = null;
			videoCompositionStages = null;

			duration = new CMTime (1, 1);

			if (composition != null)
				ProcessComposition (composition);

			if (videoComposition != null) 
				ProcessVideoComposition (videoComposition);

			if (audioMix != null) 
				ProcessAudioMix (audioMix);
		}

		private void ProcessComposition (AVMutableComposition composition)
		{
			var tracks = new List<List<APLCompositionTrackSegmentInfo>> ();
			foreach (AVCompositionTrack track in composition.Tracks) {
				var segmentInfos = new List<APLCompositionTrackSegmentInfo> ();
				foreach (AVCompositionTrackSegment segment in track.Segments) {
					var segmentInfo = new APLCompositionTrackSegmentInfo (segment, track.MediaType);
					segmentInfos.Add (segmentInfo);
				}

				tracks.Add (segmentInfos);
			}

			compositionTracks = tracks;
			duration = CMTime.GetMaximum (duration, composition.Duration);
		}

		private void ProcessVideoComposition (AVMutableVideoComposition videoComposition)
		{
			var stages = new List<APLVideoCompositionStageInfo> ();
			foreach (AVVideoCompositionInstruction instruction in videoComposition.Instructions) {
				var stage = new APLVideoCompositionStageInfo ();
				stage.TimeRange = instruction.TimeRange;

				var rampsDictionary = new Dictionary<string, List<CGPoint>> ();
				var layerNames = new List<string> ();
				foreach (AVVideoCompositionLayerInstruction layerInstruction in instruction.LayerInstructions) {
					var ramp = new List<CGPoint> ();

					CMTime startTime = CMTime.Zero;
					float startOpacity = 1f;
					float endOpacity = 1f;
					CMTimeRange timeRange = new CMTimeRange ();

					while (layerInstruction.GetOpacityRamp (startTime, ref startOpacity, ref endOpacity, ref timeRange)) {
						if (CMTime.Compare (startTime, CMTime.Zero) == 0 &&
						    CMTime.Compare (timeRange.Start, CMTime.Zero) == 1) {
							ramp.Add (new CGPoint ((float)timeRange.Start.Seconds, startOpacity));
						}
						
						CMTime endTime = CMTime.Add (timeRange.Start, timeRange.Duration);
						ramp.Add (new CGPoint ((float)endTime.Seconds, endOpacity));
						startTime = CMTime.Add (timeRange.Start, timeRange.Duration);
					}

					NSString name = new NSString (layerInstruction.TrackID.ToString ());
					layerNames.Add (name);
					rampsDictionary [name] = ramp;
				}

				if (layerNames.Count > 1) {
					stage.OpacityRamps = rampsDictionary;
				}

				stage.LayerNames = layerNames;
				stages.Add (stage);
			}

			videoCompositionStages = stages;
		}

		private void ProcessAudioMix (AVMutableAudioMix audioMix)
		{
			var mixTracks = new List<List<CGPoint>> ();
			foreach (AVAudioMixInputParameters input in audioMix.InputParameters) {
				List<CGPoint> ramp = new List<CGPoint> ();

				CMTime startTime = CMTime.Zero;
				float startVolume = 1f; 
				float endVolume = 1f;
				CMTimeRange timeRange = new CMTimeRange ();

				while (input.GetVolumeRamp (startTime, ref startVolume, ref endVolume, ref timeRange)) {
					if (CMTime.Compare (startTime, CMTime.Zero) == 0 &&
					    CMTime.Compare (timeRange.Start, CMTime.Zero) == 1) {
						ramp.Add (new CGPoint (0f, 1f));
						ramp.Add (new CGPoint ((float)timeRange.Start.Seconds, startVolume));
					}

					ramp.Add (new CGPoint ((float)timeRange.Start.Seconds, startVolume));

					CMTime endTime = CMTime.Add (timeRange.Start, timeRange.Duration);
					ramp.Add (new CGPoint ((float)endTime.Seconds, endVolume));
					startTime = CMTime.Add (timeRange.Start, timeRange.Duration);
				}

				if (CMTime.Compare (startTime, duration) == -1) {
					ramp.Add (new CGPoint ((float)duration.Seconds, endVolume));
				}

				mixTracks.Add (ramp);
			}

			audioMixTracks = mixTracks;
		}

		public override void WillMoveToSuperview (UIView newsuper)
		{
			base.WillMoveToSuperview (newsuper);
			drawingLayer.Frame = Bounds;
			drawingLayer.SetNeedsDisplay ();
		}

		public double HorizontalPositionForTime (CMTime time)
		{
			double seconds = 0.0;
			if (CMTime.Compare (time, CMTime.Zero) == 1)
				seconds = time.Seconds;

			return seconds * scaledDurationToWidth + LeftInsetToMatchTimeSlider + LeftMarginInset;
		}

		public override void Draw (CGRect rect)
		{
			base.Draw ((CGRect)rect);

			NSMutableParagraphStyle style = new NSMutableParagraphStyle ();
			style.Alignment = UITextAlignment.Center;

			int nummerOfBanners = GetNumberOfBanners ();
			int numRows = GetNumberOfRows ();

			float totalBannerHeight = nummerOfBanners * (BannerHeight + GapAfterRows);
			float rowHeight = IdealRowHeight;

			if (numRows > 0) {
                // TODO: Replaced CGRect.CGSize.Height with CGRect.Size.Height (the tool might have added .CGSize)
                // TODO: Cast nfloat to float
				float maxRowHeight = (float)((rect.Size.Height - totalBannerHeight) / numRows);
				rowHeight = Math.Min (rowHeight, maxRowHeight);
			}

            // TODO: Cast nfloat to float
			float runningTop = (float)rect.Y;
			var bannerRect = rect;
			bannerRect.Height = BannerHeight;
			bannerRect.Y = runningTop;

			var rowRect = rect;
			rowRect.Height = rowHeight;

			rowRect.X += LeftInsetToMatchTimeSlider;
			rowRect.Width -= (LeftInsetToMatchTimeSlider + RightInsetToMatchTimeSlider);
            // TODO: Replaced CGRect.CGSize.Height with CGRect.Size.Height (the tool might have added .CGSize)
            // TODO: Cast nfloat to float
			compositionRectWidth = (float)rowRect.Size.Width;

			if (duration.Seconds != 0)
				scaledDurationToWidth = compositionRectWidth / (float)duration.Seconds;
			else
				scaledDurationToWidth = 0;

			if (compositionTracks != null) {
				DrawCompositionTracks (bannerRect, rowRect, ref runningTop);
                // TODO: Cast nfloat to float
				DrawMarker (rowRect, (float)rect.Y);
			}

			if (videoCompositionStages != null)
				DrawVideoCompositionTracks (bannerRect, rowRect, ref runningTop);

			if (audioMixTracks != null) 
				DrawAudioMixTracks (bannerRect, rowRect, ref runningTop);
		}

		private void DrawCompositionTracks (CGRect bannerRect, CGRect rowRect, ref float runningTop)
		{
			bannerRect.Y = runningTop;
			CGContext context = UIGraphics.GetCurrentContext ();
            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
			context.SetFillColor (1.00f, 1.00f, 1.00f, 1.00f);
			NSString compositionTitle = new NSString ("AVComposition");
			compositionTitle.DrawString (bannerRect, UIFont.PreferredCaption1);

            // TODO: Cast nfloat to float
			runningTop += (float)bannerRect.Height;

			foreach (List<APLCompositionTrackSegmentInfo> track in compositionTracks) {
				rowRect.Y = runningTop;
				CGRect segmentRect = rowRect;
				foreach (APLCompositionTrackSegmentInfo segment in track) {
					segmentRect.Width = (float)segment.TimeRange.Duration.Seconds * scaledDurationToWidth;

					if (segment.Empty) {
                        // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
						context.SetFillColor (0.00f, 0.00f, 0.00f, 1.00f);
						DrawVerticallyCenteredInRect ("empty", segmentRect);
					} else {
						if (segment.MediaType == AVMediaType.Video) {
                            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
							context.SetFillColor (0.00f, 0.36f, 0.36f, 1.00f); // blue-green
                            // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
							context.SetStrokeColor (0.00f, 0.50f, 0.50f, 1.00f); // brigher blue-green
						} else {
                            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
							context.SetFillColor (0.00f, 0.24f, 0.36f, 1.00f); // bluer-green
                            // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
							context.SetStrokeColor (0.00f, 0.33f, 0.60f, 1.00f); // brigher bluer-green
						}

						context.SetLineWidth (2f);
						segmentRect = segmentRect.Inset (3f, 3f);
						context.AddRect (segmentRect);
						context.DrawPath (CGPathDrawingMode.FillStroke);

                        // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
						context.SetFillColor (0.00f, 0.00f, 0.00f, 1.00f); // white
						DrawVerticallyCenteredInRect (segment.Description, segmentRect);
					}

					segmentRect.X += segmentRect.Width;
				}
                // TODO: Cast nfloat to float
				runningTop += (float)rowRect.Height;
			}
			runningTop += GapAfterRows;
		}

		private void DrawAudioMixTracks (CGRect bannerRect, CGRect rowRect, ref float runningTop)
		{
			bannerRect.Y = runningTop;
			CGContext context = UIGraphics.GetCurrentContext ();
            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
			context.SetFillColor (1.00f, 1.00f, 1.00f, 1.00f); // white

			NSString compositionTitle = new NSString ("AVAudioMix");
			compositionTitle.DrawString (bannerRect, UIFont.PreferredCaption1);
            // TODO: Cast nfloat to float
			runningTop += (float)bannerRect.Height;

			foreach (List<CGPoint> mixTrack in audioMixTracks) {
				rowRect.Y = runningTop;

				CGRect rampRect = rowRect;
				rampRect.Width = (float)duration.Seconds * scaledDurationToWidth;
				rampRect = rampRect.Inset (3f, 3f);

                // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
				context.SetFillColor (0.55f, 0.02f, 0.02f, 1.00f); // darker red
                // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
				context.SetStrokeColor (0.87f, 0.10f, 0.10f, 1.00f); // brighter red
				context.SetLineWidth (2f);
				context.AddRect (rampRect);
				context.DrawPath (CGPathDrawingMode.FillStroke);

				context.BeginPath ();
                // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
				context.SetStrokeColor (0.95f, 0.68f, 0.09f, 1.00f); // yellow
				context.SetLineWidth (3f);
				bool firstPoint = true;

				foreach (CGPoint pointValue in mixTrack) {
					CGPoint timeVolumePoint = pointValue;
					CGPoint pointInRow = new CGPoint ();

					pointInRow.X = rampRect.X + timeVolumePoint.X * scaledDurationToWidth;
					pointInRow.Y = rampRect.Y + (0.9f - 0.8f * timeVolumePoint.Y) * rampRect.Height;
                    // TODO: Cast double to nfloat
					pointInRow.X = (nfloat)Math.Max (pointInRow.X, rampRect.GetMinX ());
					pointInRow.X = (nfloat)Math.Min (pointInRow.X, rampRect.GetMaxX ());

					if (firstPoint) {
						context.MoveTo (pointInRow.X, pointInRow.Y);
						firstPoint = false;
					} else {
						context.AddLineToPoint (pointInRow.X, pointInRow.Y);
					}
				}
				context.StrokePath ();
                // TODO: Cast nfloat to float
				runningTop += (float)rowRect.Height;
			}
             
			runningTop += GapAfterRows;
		}

		private void DrawMarker (CGRect rowRect, float position)
		{
			if (Layer.Sublayers != null) {
				Layer.Sublayers = new CALayer[0];
			}

			var visibleRect = Layer.Bounds;
			var currentTimeRect = visibleRect;

			// The red band of the timeMaker will be 7 pixels wide
			currentTimeRect.X = 0f;
			currentTimeRect.Width = 7f;

			var timeMarkerRedBandLayer = new CAShapeLayer ();
			timeMarkerRedBandLayer.Frame = currentTimeRect;
			timeMarkerRedBandLayer.Position = new CGPoint (rowRect.X, Bounds.Height / 2f);

			var linePath = CGPath.FromRect (currentTimeRect);
			timeMarkerRedBandLayer.FillColor = UIColor.FromRGBA ((nfloat)1.00f, (nfloat)0.00f, (nfloat)0.00f, (nfloat)0.50f).CGColor;

			timeMarkerRedBandLayer.Path = linePath;

			currentTimeRect.X = 0f;
			currentTimeRect.Width = 1f;

			CAShapeLayer timeMarkerWhiteLineLayer = new CAShapeLayer ();
			timeMarkerWhiteLineLayer.Frame = currentTimeRect;
			timeMarkerWhiteLineLayer.Position = new CGPoint (3f, Bounds.Height / 2f);

			CGPath whiteLinePath = CGPath.FromRect (currentTimeRect);
			timeMarkerWhiteLineLayer.FillColor = UIColor.FromRGBA ((nfloat)1.00f, (nfloat)1.00f, (nfloat)1.00f, (nfloat)1.00f).CGColor;
			timeMarkerWhiteLineLayer.Path = whiteLinePath;

			timeMarkerRedBandLayer.AddSublayer (timeMarkerWhiteLineLayer);
			CABasicAnimation scrubbingAnimation = new CABasicAnimation ();
			scrubbingAnimation.KeyPath = "position.x";

			scrubbingAnimation.From = new NSNumber (HorizontalPositionForTime (CMTime.Zero));
			scrubbingAnimation.To = new NSNumber (HorizontalPositionForTime (duration));
			scrubbingAnimation.RemovedOnCompletion = false;
			scrubbingAnimation.BeginTime = 0.000000001;
			scrubbingAnimation.Duration = duration.Seconds;
			scrubbingAnimation.FillMode = CAFillMode.Both;
			timeMarkerRedBandLayer.AddAnimation (scrubbingAnimation, null);

			Console.WriteLine ("Duration in  seconds - " + Player.CurrentItem.Asset.Duration.Seconds);
			var syncLayer = new AVSynchronizedLayer () {
				PlayerItem = Player.CurrentItem,
			};
			syncLayer.AddSublayer (timeMarkerRedBandLayer);
			Layer.AddSublayer (syncLayer);
		}

		private void DrawVideoCompositionTracks (CGRect bannerRect, CGRect rowRect, ref float runningTop)
		{
			bannerRect.Y = runningTop;
			var context = UIGraphics.GetCurrentContext ();
            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
			context.SetFillColor (1.00f, 1.00f, 1.00f, 1.00f);
			var compositionTitle = new NSString ("AVComposition");
			compositionTitle.DrawString (bannerRect, UIFont.PreferredCaption1);

            // TODO: Cast nfloat to float
			runningTop += (float)bannerRect.Height;
			rowRect.Y = runningTop;
			CGRect stageRect = rowRect;

			foreach (APLVideoCompositionStageInfo stage in videoCompositionStages) {
				stageRect.Width = (float)stage.TimeRange.Duration.Seconds * scaledDurationToWidth;
				int layerCount = stage.LayerNames.Count;
				CGRect layerRect = stageRect;

				if (layerCount > 0)
					layerRect.Height /= layerCount;

				foreach (string layerName in stage.LayerNames) {
					CGRect bufferRect = layerRect;
					int intValueOfName; 
					Int32.TryParse (layerName, out intValueOfName); 
					if (intValueOfName % 2 == 1) {
                        // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
						context.SetFillColor (0.55f, 0.02f, 0.02f, 1.00f); // darker red
                        // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
						context.SetStrokeColor (0.87f, 0.10f, 0.10f, 1.00f); // brighter red
					} else {
                        // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
                        context.SetFillColor(0.00f, 0.40f, 0.76f, 1.00f); // darker blue
                        // TODO: Change CGContext.SetRGBStrokeColor to .SetStrokeColor
						context.SetStrokeColor (0.00f, 0.67f, 1.00f, 1.00f); // brighter blue
					}
			
					context.SetLineWidth (2f);
					bufferRect = bufferRect.Inset (2f, 3f);
					context.AddRect (bufferRect);
					context.DrawPath (CGPathDrawingMode.FillStroke);

                    // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
					context.SetFillColor (0.00f, 0.00f, 0.00f, 1.00f); // white
					DrawVerticallyCenteredInRect (layerName, bufferRect);

					// Draw the opacity ramps for each layer as per the layerInstructions
					List<CGPoint> rampArray = new List<CGPoint> ();

					if (stage.OpacityRamps != null)
						rampArray = stage.OpacityRamps [layerName];

					if (rampArray.Count > 0) {
						CGRect rampRect = bufferRect;
						rampRect.Width = (float)duration.Seconds * scaledDurationToWidth;
						rampRect = rampRect.Inset (3f, 3f);

						context.BeginPath ();
                        // TODO: Change CGContext.SetRGBStokeColor to .SetStrokeColor
						context.SetStrokeColor (0.95f, 0.68f, 0.09f, 1.00f); // yellow
						context.SetLineWidth (2f);
						bool firstPoint = true;

						foreach (CGPoint point in rampArray) {
							CGPoint timeVolumePoint = point;
							CGPoint pointInRow = new CGPoint ();

							pointInRow.X = (float)HorizontalPositionForTime (CMTime.FromSeconds (timeVolumePoint.X, 1)) - 9.0f;
							pointInRow.Y = rampRect.Y + (0.9f - 0.8f * timeVolumePoint.Y) * rampRect.Height;

                            // TODO: Cast double to nfloat
							pointInRow.X = (nfloat)Math.Max (pointInRow.X, rampRect.GetMinX ());
							pointInRow.X = (nfloat)Math.Min (pointInRow.X, rampRect.GetMaxX ());

							if (firstPoint) {
								context.MoveTo (pointInRow.X, pointInRow.Y);
								firstPoint = false;
							} else
								context.AddLineToPoint (pointInRow.X, pointInRow.Y);
						}
						context.StrokePath ();
					}
					layerRect.Y += layerRect.Height;
				}
				stageRect.X += stageRect.Width;
			}
            // TODO: Cast nfloat to float
			runningTop += (float)rowRect.Height;
			runningTop += GapAfterRows;
		}

		private void DrawVerticallyCenteredInRect (string text, CGRect rect)
		{
            CGContext context = UIGraphics.GetCurrentContext();
            // TODO: Change CGContext.SetRGBFillColor to .SetFillColor
			context.SetFillColor (1.00f, 1.00f, 1.00f, 1.00f);
			NSString title = new NSString (text);
			rect.Y += rect.Height / 2f - UIFont.PreferredCaption1.xHeight;
			title.DrawString (rect, UIFont.PreferredCaption1, 
			                  UILineBreakMode.CharacterWrap, 
			                  UITextAlignment.Center);
		}

		private int GetNumberOfRows ()
		{
			int nummerOfRows = 0;

			if (compositionTracks != null)
				nummerOfRows += (int)compositionTracks.Count;

			if (audioMixTracks != null)
				nummerOfRows += (int)audioMixTracks.Count;

			if (videoCompositionStages != null)
				nummerOfRows += (int)videoCompositionStages.Count;

			return nummerOfRows;
		}

		private int GetNumberOfBanners ()
		{
			int nummerOfBanners = 0;

			if (compositionTracks != null)
				nummerOfBanners++;

			if (audioMixTracks != null)
				nummerOfBanners++;

			if (videoCompositionStages != null)
				nummerOfBanners++;

			return nummerOfBanners;
		}
	}
}
