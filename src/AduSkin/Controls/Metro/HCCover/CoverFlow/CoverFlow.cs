using AduSkin.Controls.Data;
using AduSkin.Utility.Element;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace AduSkin.Controls.Metro
{
   /// <summary>
   ///     封面流
   /// </summary>
   [TemplatePart(Name = ElementViewport3D, Type = typeof(Viewport3D))]
   [TemplatePart(Name = ElementCamera, Type = typeof(ProjectionCamera))]
   [TemplatePart(Name = ElementVisualParent, Type = typeof(ModelVisual3D))]
   public class CoverFlow : Control
   {
      public CoverFlow()
      {
         Utility.Refresh(this);
         this.ItemsSource = new List<object>();
      }

      static CoverFlow()
      {
         ElementBase.DefaultStyle<CoverFlow>(DefaultStyleKeyProperty);
      }
      private const string ElementViewport3D = "PART_Viewport3D";

      private const string ElementCamera = "PART_Camera";

      private const string ElementVisualParent = "PART_VisualParent";

      /// <summary>
      ///     最大显示数量的一半
      /// </summary>
      private const int MaxShowCountHalf = 7;

      /// <summary>
      ///     页码
      /// </summary>
      public static readonly DependencyProperty PageIndexProperty = DependencyProperty.Register("PageIndex", typeof(int), typeof(CoverFlow), new PropertyMetadata(ValueBoxes.Int0Box, OnPageIndexChanged, CoercePageIndex));

      private static object CoercePageIndex(DependencyObject d, object baseValue)
      {
         var ctl = (CoverFlow)d;
         var v = (int)baseValue;

         if (v < 0)
         {
            return 0;
         }
         if (v >= ctl.Count)
         {
            return ctl.Count - 1;
         }
         return v;
      }

      private static void OnPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         var ctl = (CoverFlow)d;
         ctl.UpdateIndex((int)e.NewValue, (int)e.OldValue);
         ctl.OnIndexChanged((int)e.OldValue, (int)e.NewValue);
      }
      /// <summary>
      /// 切换事件
      /// </summary>
      public static RoutedEvent IndexChangedEvent = EventManager.RegisterRoutedEvent("IndexChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(CoverFlow));
      public event RoutedPropertyChangedEventHandler<int> IndexChanged
      {
         add { AddHandler(IndexChangedEvent, value); }
         remove { RemoveHandler(IndexChangedEvent, value); }
      }

      public virtual void OnIndexChanged(int oldValue, int newValue)
      {
         RoutedPropertyChangedEventArgs<int> arg = new RoutedPropertyChangedEventArgs<int>(oldValue, newValue, IndexChangedEvent);
         this.RaiseEvent(arg);
      }

      /// <summary>
      ///     是否循环
      /// </summary>
      public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
            "Loop", typeof(bool), typeof(CoverFlow), new PropertyMetadata(ValueBoxes.FalseBox));

      /// <summary>
      ///     存储所有的内容
      /// </summary>
      //private readonly Dictionary<int, object> _contentDic = new Dictionary<int, object>();

      /// <summary>
      ///     当前在显示范围内的项
      /// </summary>
      private readonly Dictionary<int, CoverFlowItem> _itemShowList = new Dictionary<int, CoverFlowItem>();

      public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(CoverFlow));

      /// <summary>
      /// 轮播的Item的数据模板
      /// </summary>
      public DataTemplate ItemTemplate
      {
         get { return (DataTemplate)GetValue(ItemTemplateProperty); }
         set { SetValue(ItemTemplateProperty, value); }
      }

      public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(CoverFlow));

      /// <summary>
      /// 轮播数据源
      /// </summary>
      public IEnumerable ItemsSource
      {
         get
         {
            return (IEnumerable)GetValue(ItemsSourceProperty);
         }
         set
         {
            SetValue(ItemsSourceProperty, value);
         }
      }

      public static readonly DependencyProperty CurrentItemProperty = DependencyProperty.Register("CurrentItem", typeof(object), typeof(CoverFlow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      /// <summary>
      /// 轮播数据源
      /// </summary>
      public object CurrentItem
      {
         get
         {
            return (object)GetValue(CurrentItemProperty);
         }
         set
         {
            SetValue(CurrentItemProperty, value);
         }
      }

      private int _Count;
      /// <summary>
      /// 属性.
      /// </summary>
      public int Count
      {
         get
         {
            var items = (ItemsSource as IList);
            return items.Count;

         }
      }

      /// <summary>
      ///     相机
      /// </summary>
      private ProjectionCamera _camera;

      /// <summary>
      ///     3d画布
      /// </summary>
      private Viewport3D _viewport3D;

      /// <summary>
      ///     项容器
      /// </summary>
      private ModelVisual3D _visualParent;

      /// <summary>
      ///     显示范围内第一个项的编号
      /// </summary>
      private int _firstShowIndex;

      /// <summary>
      ///     显示范围内最后一个项的编号
      /// </summary>
      private int _lastShowIndex;

      /// <summary>
      ///     跳转编号
      /// </summary>
      private int _jumpToIndex = -1;

      /// <summary>
      ///     页码
      /// </summary>
      public int PageIndex
      {
         get => (int)GetValue(PageIndexProperty);
         internal set
         {
            CurrentItem = _itemShowList[value].CurrentItemData;
            SetValue(PageIndexProperty, value);
         }
      }

      /// <summary>
      ///     是否循环
      /// </summary>
      public bool Loop
      {
         get => (bool)GetValue(LoopProperty);
         set => SetValue(LoopProperty, value);
      }

      public override void OnApplyTemplate()
      {
         if (_viewport3D != null)
         {
            _viewport3D.Children.Clear();
            _itemShowList.Clear();
            _viewport3D.MouseLeftButtonDown -= Viewport3D_MouseLeftButtonDown;
         }

         base.OnApplyTemplate();

         _viewport3D = GetTemplateChild(ElementViewport3D) as Viewport3D;
         if (_viewport3D != null)
         {
            _viewport3D.MouseLeftButtonDown += Viewport3D_MouseLeftButtonDown;
         }

         _camera = GetTemplateChild(ElementCamera) as ProjectionCamera;
         _visualParent = GetTemplateChild(ElementVisualParent) as ModelVisual3D;

         UpdateShowRange();
         if (_jumpToIndex > 0)
         {
            PageIndex = _jumpToIndex;
            _jumpToIndex = -1;
         }
         _camera.Position = new Point3D(CoverFlowItem.Interval * PageIndex, _camera.Position.Y, _camera.Position.Z);
      }

      /// <summary>
      ///     批量添加资源
      /// </summary>
      /// <param name="contentList"></param>
      //public void AddRange(IEnumerable<object> contentList)
      //{
      //    foreach (var content in contentList)
      //    {
      //        _contentDic.Add(_contentDic.Count, content);
      //    }
      //}

      /// <summary>
      ///     添加一项资源
      /// </summary>
      /// <param name="uriString"></param>
      //public void Add(string uriString) => _contentDic.Add(_contentDic.Count, new Uri(uriString));

      /// <summary>
      ///     添加一项资源
      /// </summary>
      /// <param name="uri"></param>
      //public void Add(Uri uri) => _contentDic.Add(_contentDic.Count, uri);

      /// <summary>
      ///     跳转
      /// </summary>
      public void JumpTo(int index) => _jumpToIndex = index;

      protected override void OnMouseWheel(MouseWheelEventArgs e)
      {
         base.OnMouseWheel(e);

         if (e.Delta < 0)
         {
            var index = PageIndex + 1;
            PageIndex = index >= Count ? Loop ? 0 : Count - 1 : index;
         }
         else
         {
            var index = PageIndex - 1;
            PageIndex = index < 0 ? Loop ? Count - 1 : 0 : index;
         }

         e.Handled = true;
      }

      /// <summary>
      ///     删除指定位置的项
      /// </summary>
      /// <param name="pos"></param>
      private void Remove(int pos)
      {
         if (!_itemShowList.TryGetValue(pos, out var item)) return;
         _visualParent.Children.Remove(item);
         _itemShowList.Remove(pos);
      }

      /// <summary>
      ///     移动项到指定的位置
      /// </summary>
      /// <param name="index"></param>
      /// <param name="animated"></param>
      private void Move(int index, bool animated) => _itemShowList[index].Move(PageIndex, animated);

      private void Viewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         var result = (RayMeshGeometry3DHitTestResult)VisualTreeHelper.HitTest(_viewport3D, e.GetPosition(_viewport3D));
         if (result != null)
         {
            foreach (var item in _itemShowList.Values)
            {
               if (item.HitTest(result.MeshHit))
               {
                  PageIndex = item.Index;
                  break;
               }
            }
         }
      }

      /// <summary>
      ///     更新项的位置
      /// </summary>
      /// <param name="newIndex"></param>
      /// <param name="oldIndex"></param>
      private void UpdateIndex(int newIndex, int oldIndex)
      {
         var animate = Math.Abs(newIndex - oldIndex) < MaxShowCountHalf;
         UpdateShowRange();
         if (newIndex > oldIndex)
         {
            if (oldIndex < _firstShowIndex)
            {
               oldIndex = _firstShowIndex;
            }
            for (var i = oldIndex; i <= newIndex; i++)
            {
               Move(i, animate);
            }
         }
         else
         {
            if (oldIndex > _lastShowIndex)
            {
               oldIndex = _lastShowIndex;
            }
            for (var i = oldIndex; i >= newIndex; i--)
            {
               Move(i, animate);
            }
         }
         _camera.Position = new Point3D(CoverFlowItem.Interval * newIndex, _camera.Position.Y,
             _camera.Position.Z);
      }

      /// <summary>
      ///     更新显示范围
      /// </summary>
      private void UpdateShowRange()
      {
         if (this.ItemsSource == null)
            return;
         var newFirstShowIndex = Math.Max(PageIndex - MaxShowCountHalf, 0);
         var newLastShowIndex = Math.Min(PageIndex + MaxShowCountHalf, Count - 1);

         if (_firstShowIndex < newFirstShowIndex)
         {
            for (var i = _firstShowIndex; i < newFirstShowIndex; i++)
            {
               Remove(i);
            }
         }
         else if (newLastShowIndex < _lastShowIndex)
         {
            for (var i = newLastShowIndex; i < _lastShowIndex; i++)
            {
               Remove(i);
            }
         }

         for (var i = newFirstShowIndex; i <= newLastShowIndex; i++)
         {
            if (!_itemShowList.ContainsKey(i))
            {
               var cover = CreateCoverFlowItem(i, (ItemsSource as IList)[i]);
               _itemShowList[i] = cover;
               _visualParent.Children.Add(cover);
            }
         }

         _firstShowIndex = newFirstShowIndex;
         _lastShowIndex = newLastShowIndex;

      }

      private CoverFlowItem CreateCoverFlowItem(int index, object content)
      {
         if (content is Uri uri)
         {
            try
            {
               return new CoverFlowItem(index, PageIndex, content, new Image
               {
                  Source = BitmapFrame.Create(uri, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand)
               });
            }
            catch
            {
               return new CoverFlowItem(index, PageIndex, content, new ContentControl());
            }
         }
         var contentControl = new ContentControl
         {
            Content = content
         };
         contentControl.Content = content;
         contentControl.HorizontalAlignment = HorizontalAlignment.Stretch;
         contentControl.HorizontalContentAlignment = HorizontalAlignment.Center;
         contentControl.VerticalContentAlignment = VerticalAlignment.Center;
         contentControl.VerticalAlignment = VerticalAlignment.Stretch;
         contentControl.ContentTemplate = this.ItemTemplate;
         return new CoverFlowItem(index, PageIndex, content, contentControl);
      }
   }
}