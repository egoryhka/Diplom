using Diplom.DataModule;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Diplom.UI
{
	public class ZoomBorder : Border
	{
		private UIElement child = null;
		private ScrollViewer parent = null;

		private Point origin;
		private Point start;

		public Label label;
		public Image image;

		public double zoom = 1.0d;

		private TranslateTransform GetTranslateTransform(UIElement element)
		{
			return (TranslateTransform)((TransformGroup)element.RenderTransform)
			  .Children.First(tr => tr is TranslateTransform);
		}

		private ScaleTransform GetScaleTransform(UIElement element)
		{
			return (ScaleTransform)((TransformGroup)element.RenderTransform)
			  .Children.First(tr => tr is ScaleTransform);
		}

		public override UIElement Child
		{
			get { return base.Child; }
			set
			{
				if (value != null && value != this.Child)
					this.Initialize(value);
				base.Child = value;
			}
		}

		public void Initialize(UIElement element)
		{
			this.child = element;
			if (child != null)
			{
				TransformGroup group = new TransformGroup();
				ScaleTransform st = new ScaleTransform();
				group.Children.Add(st);
				TranslateTransform tt = new TranslateTransform();
				group.Children.Add(tt);
				child.RenderTransform = group;
				child.RenderTransformOrigin = new Point(0.0, 0.0);
				this.MouseWheel += child_MouseWheel;
				this.MouseLeftButtonDown += child_MouseLeftButtonDown;
				this.MouseLeftButtonUp += child_MouseLeftButtonUp;
				this.MouseMove += child_MouseMove;
			}
			parent = Parent as ScrollViewer;
		}

		public void Reset()
		{
			if (child != null)
			{
				// reset zoom
				var st = GetScaleTransform(child);
				st.ScaleX = 1.0;
				st.ScaleY = 1.0;

				// reset pan
				var tt = GetTranslateTransform(child);
				tt.X = 0.0;
				tt.Y = 0.0;

				label.Content = Math.Round(image.Source.Width / 2d * DataManager.CurrentData.Settings.NmPpx / st.ScaleX, 5).ToString() + " µm";
			}
		}

		#region Child Events
		private void child_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (child != null)
			{
				e.Handled = true; // to prevent scrollViewer event

				var st = GetScaleTransform(child);
				var tt = GetTranslateTransform(child);

				double zoom = e.Delta > 0 ? .2 : -.2;
				double zoomCorrected = zoom /** st.ScaleX*/;
				double nextScaleX = st.ScaleX + zoomCorrected;
				double nextScaleY = st.ScaleY + zoomCorrected;

				if (e.Delta < 0 && (nextScaleX <= 1 || nextScaleY <= 1))
				{
					Reset();
					return;
				}

				Point relative = e.GetPosition(child);
				double absoluteX = relative.X * st.ScaleX + tt.X;
				double absoluteY = relative.Y * st.ScaleY + tt.Y;

				st.ScaleX += zoomCorrected;
				st.ScaleY += zoomCorrected;

				double childWidth = (double)child.GetValue(ActualWidthProperty);
				double childHeight = (double)child.GetValue(ActualHeightProperty);

				tt.X = Math.Clamp(absoluteX - relative.X * st.ScaleX, childWidth - childWidth * st.ScaleX, 0);
				tt.Y = Math.Clamp(absoluteY - relative.Y * st.ScaleY, childHeight - childHeight * st.ScaleY, 0);

				label.Content = Math.Round(image.Source.Width / 2d * DataManager.CurrentData.Settings.NmPpx / st.ScaleX, 5).ToString() + " µm";
			}
		}

		private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (child != null)
			{
				var tt = GetTranslateTransform(child);
				start = e.GetPosition(this);
				origin = new Point(tt.X, tt.Y);
				this.Cursor = Cursors.Hand;
				child.CaptureMouse();
			}
		}

		private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (child != null)
			{
				child.ReleaseMouseCapture();
				this.Cursor = Cursors.Arrow;
			}
		}

		private void child_MouseMove(object sender, MouseEventArgs e)
		{
			if (child != null)
			{
				if (child.IsMouseCaptured)
				{
					var st = GetScaleTransform(child);

					var tt = GetTranslateTransform(child);
					Vector v = start - e.GetPosition(this);

					double childWidth = (double)child.GetValue(ActualWidthProperty);
					double childHeight = (double)child.GetValue(ActualHeightProperty);

					var parentScroll = Parent as ScrollViewer;
					parentScroll.ScrollToVerticalOffset(parentScroll.VerticalOffset + v.Y);
					parentScroll.ScrollToHorizontalOffset(parentScroll.HorizontalOffset + v.X);

					tt.X = Math.Clamp(origin.X - v.X, childWidth - childWidth * st.ScaleX, 0);
					tt.Y = Math.Clamp(origin.Y - v.Y, childHeight - childHeight * st.ScaleY, 0);
				}
			}
		}
		#endregion
	}
}