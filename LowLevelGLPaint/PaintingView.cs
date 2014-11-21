using System;
using ObjCRuntime;
using Foundation;
using System.Runtime.InteropServices;
using CoreGraphics;
using OpenTK.Platform;
using CoreAnimation;
using OpenTK.Graphics.ES11;
using UIKit;
using CoreGraphics;

namespace LowLevelGLPaint
{
	public class PaintingView : EAGLView
	{
		public const float BrushOpacity = 1.0f / 3.0f;
		public const int BrushPixelStep = 3;
		public const int BrushScale = 2;
		public const float Luminosity = 0.75f;
		public const float Saturation = 1.0f;

		uint brushTexture, drawingTexture, drawingFramebuffer;
		bool firstTouch;

		CGPoint Location;
		CGPoint PreviousLocation;
		
		[Foundation.Export("layerClass")]
		public static Class LayerClass ()
		{
			return new Class (typeof (CAEAGLLayer));
		}

		public PaintingView (CGRect frame)
			: base (frame, All.Rgb565Oes, 0, true)
		{
			SetCurrentContext ();
			var brushImage = UIImage.FromFile ("Particle.png").CGImage;
			var width = brushImage.Width;
			var height = brushImage.Height;
			if (brushImage != null) {
                // TODO: Cast nint to int
				IntPtr brushData = Marshal.AllocHGlobal ((int)(width * height * 4));
				if (brushData == IntPtr.Zero)
					throw new OutOfMemoryException ();
				try {
					using (var brushContext = new CGBitmapContext (brushData,
							(int)width, (int)width, 8, (int)width * 4, brushImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast)) {
						brushContext.DrawImage ((CGRect)new CGRect (0.0f, 0.0f, (float) width, (float) height), (CGImage)brushImage);
					}

					GL.GenTextures (1, out brushTexture);
					GL.BindTexture (All.Texture2D, brushTexture);
					GL.TexImage2D (All.Texture2D, 0, (int) All.Rgba, (int)width, (int)height, 0, All.Rgba, All.UnsignedByte, brushData);
				}
				finally {
					Marshal.FreeHGlobal (brushData);
				}
				GL.TexParameter (All.Texture2D, All.TextureMinFilter, (int) All.Linear);
				GL.Enable (All.Texture2D);
				GL.BlendFunc (All.SrcAlpha, All.One);
				GL.Enable (All.Blend);
			}
			GL.Disable (All.Dither);
			GL.MatrixMode (All.Projection);
            // TODO: Cast nfloat to float
			GL.Ortho (0, (float)frame.Width, 0, (float)frame.Height, -1, 1);
			GL.MatrixMode (All.Modelview);
			GL.Enable (All.Texture2D);
			GL.EnableClientState (All.VertexArray);
			GL.Enable (All.Blend);
			GL.BlendFunc (All.SrcAlpha, All.One);
			GL.Enable (All.PointSpriteOes);
			GL.TexEnv (All.PointSpriteOes, All.CoordReplaceOes, (float) All.True);
			GL.PointSize (width / BrushScale);

			Erase ();

			PerformSelector (new Selector ("playback"), null, 0.2f);
		}

		~PaintingView ()
		{
			Dispose (false);
		}

		protected override void Dispose (bool disposing)
		{
			GL.Oes.DeleteFramebuffers (1, ref drawingFramebuffer);
			GL.DeleteTextures (1, ref drawingTexture);
		}

		public void Erase ()
		{
			GL.Clear ((uint) All.ColorBufferBit);

			SwapBuffers ();
		}

		float[] vertexBuffer;
		int vertexMax = 64;

		private void RenderLineFromPoint (CGPoint start, CGPoint end)
		{
			int vertexCount = 0;
			if (vertexBuffer == null) {
				vertexBuffer = new float [vertexMax * 2];
			}
			var count = Math.Max (Math.Ceiling (Math.Sqrt ((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y)) / BrushPixelStep),
					1);
			for (int i = 0; i < count; ++i, ++vertexCount) {
				if (vertexCount == vertexMax) {
					vertexMax *= 2;
					Array.Resize (ref vertexBuffer, vertexMax * 2);
				}
				vertexBuffer [2 * vertexCount + 0] = (float)(start.X + (end.X - start.X) * (float) i / (float) count);
				vertexBuffer [2 * vertexCount + 1] = (float)(start.Y + (end.Y - start.Y) * (float) i / (float) count);
			}
			GL.VertexPointer (2, All.Float, 0, vertexBuffer);
			GL.DrawArrays (All.Points, 0, vertexCount);

			SwapBuffers ();
		}

		int dataofs = 0;

		[Foundation.Export("playback")]
		void Playback ()
		{
			CGPoint [] points = ShakeMe.Data [dataofs];

			for (int i = 0; i < points.Length - 1; i++)
				RenderLineFromPoint (points [i], points [i + 1]);

			if (dataofs < ShakeMe.Data.Count - 1) {
				dataofs ++;
				PerformSelector (new Selector ("playback"), null, 0.01f);
			}
		}

		public override void TouchesBegan (Foundation.NSSet touches, UIKit.UIEvent e)
		{
			var bounds = Bounds;
			var touch = (UITouch) e.TouchesForView (this).AnyObject;
			firstTouch = true;
			Location = (CGPoint)touch.LocationInView ((UIView)this);
			Location.Y = bounds.Height - Location.Y;
		}

		public override void TouchesMoved (Foundation.NSSet touches, UIKit.UIEvent e)
		{
			var bounds = Bounds;
			var touch = (UITouch) e.TouchesForView (this).AnyObject;

			if (firstTouch) {
				firstTouch = false;
				PreviousLocation = (CGPoint)touch.PreviousLocationInView ((UIView)this);
				PreviousLocation.Y = bounds.Height - PreviousLocation.Y;
			}
			else {
				Location = (CGPoint)touch.LocationInView ((UIView)this);
				Location.Y = bounds.Height - Location.Y;
				PreviousLocation = (CGPoint)touch.PreviousLocationInView ((UIView)this);
				PreviousLocation.Y = bounds.Height - PreviousLocation.Y;
			}
			RenderLineFromPoint (PreviousLocation, Location);
		}

		public override void TouchesEnded (Foundation.NSSet touches, UIKit.UIEvent e)
		{
			var bounds = Bounds;
			var touch = (UITouch) e.TouchesForView (this).AnyObject;
			if (firstTouch) {
				firstTouch = false;
				PreviousLocation = (CGPoint)touch.PreviousLocationInView ((UIView)this);
				PreviousLocation.Y = bounds.Height - PreviousLocation.Y;
				RenderLineFromPoint (PreviousLocation, Location);
			}
		}

		public override void TouchesCancelled (Foundation.NSSet touches, UIKit.UIEvent e)
		{
		}
	}
}

