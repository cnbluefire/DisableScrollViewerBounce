using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace DisableScrollViewerBounce.Behavoirs
{
    public class ScrollViewerBounceBehavior : Behavior<ScrollViewer>
    {
        private long contentChangedToken = -1;
        private CompositionPropertySet scrollPropSet;
        private CompositionPropertySet propSet;
        private ExpressionAnimation exp;
        private UIElement oldContent;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject is ScrollViewer scrollViewer)
            {
                scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
                oldContent = scrollViewer.Content as UIElement;
                contentChangedToken = scrollViewer.RegisterPropertyChangedCallback(ContentControl.ContentProperty, OnContentChanged);
                scrollPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                propSet = Window.Current.Compositor.CreatePropertySet();
                propSet.InsertScalar("viewportheight", (float)scrollViewer.ViewportHeight);
                OnIsBounceEnabledChanged();
            }
        }

        private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                propSet.InsertScalar("viewportheight", (float)scrollViewer.ViewportHeight);
            }
        }

        private void OnContentChanged(DependencyObject sender, DependencyProperty dp)
        {
            StopAnimation(oldContent);
            oldContent = null;
            if (AssociatedObject is ScrollViewer scrollViewer)
            {
                oldContent = scrollViewer.Content as UIElement;
                OnIsBounceEnabledChanged();
            }
        }

        private void StartAnimation(UIElement element)
        {
            if (element != null)
            {
                if (exp == null)
                {
                    var bottomExp = "(scroll.Translation.Y < prop.viewportheight - this.target.Size.Y ? (prop.viewportheight - this.target.Size.Y - scroll.Translation.Y) : 0)";
                    var topExp = $"scroll.Translation.Y > 0 ? (-scroll.Translation.Y) : {bottomExp}";
                    exp = Window.Current.Compositor.CreateExpressionAnimation($"{topExp}");
                    exp.SetReferenceParameter("prop", propSet);
                    exp.SetReferenceParameter("scroll", scrollPropSet);
                }
                ElementCompositionPreview.GetElementVisual(element).StartAnimation("Offset.Y", exp);
            }
        }

        private void StopAnimation(UIElement element)
        {
            if (element != null)
            {
                ElementCompositionPreview.GetElementVisual(element).StopAnimation("Offset.Y");
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            StopAnimation(oldContent);
            oldContent = null;

            if (AssociatedObject is ScrollViewer scrollViewer)
            {
                if (contentChangedToken > -1)
                {
                    scrollViewer.UnregisterPropertyChangedCallback(ContentControl.ContentProperty, contentChangedToken);
                }
            }
        }

        protected void OnIsBounceEnabledChanged()
        {
            if (IsBounceEnabled)
            {
                StopAnimation(oldContent);
            }
            else
            {
                StartAnimation(oldContent);
            }
        }

        public bool IsBounceEnabled
        {
            get { return (bool)GetValue(IsBounceEnabledProperty); }
            set { SetValue(IsBounceEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsBounceEnabledProperty =
            DependencyProperty.Register("IsBounceEnabled", typeof(bool), typeof(ScrollViewerBounceBehavior), new PropertyMetadata(false, OnIsBounceEnabledChanged));

        private static void OnIsBounceEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewerBounceBehavior behavior)
            {
                if (e.NewValue != e.OldValue)
                {
                    behavior.OnIsBounceEnabledChanged();
                }
            }
        }
    }
}
