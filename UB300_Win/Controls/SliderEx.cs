using System.Windows;
using System.Windows.Controls;

namespace Cerevo.UB300_Win.Controls {
    public class SliderEx : Slider {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(SliderEx),
            new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.AffectsRender));

        public object Content {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentStringFormatProperty = DependencyProperty.Register(
            "ContentStringFormat",
            typeof(string),
            typeof(SliderEx),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.AffectsRender));

        public string ContentStringFormat {
            get { return (string)GetValue(ContentStringFormatProperty); }
            set { SetValue(ContentStringFormatProperty, value); }
        }

        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
            "ContentTemplate",
            typeof(DataTemplate),
            typeof(SliderEx),
            new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.AffectsRender));

        public DataTemplate ContentTemplate {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty ContentTemplateSelectorProperty = DependencyProperty.Register(
            "ContentTemplateSelector",
            typeof(DataTemplateSelector),
            typeof(SliderEx),
            new FrameworkPropertyMetadata(default(DataTemplateSelector), FrameworkPropertyMetadataOptions.AffectsRender));

        public DataTemplateSelector ContentTemplateSelector {
            get { return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty); }
            set { SetValue(ContentTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive",
            typeof(bool),
            typeof(SliderEx),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsActive {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
    }
}
