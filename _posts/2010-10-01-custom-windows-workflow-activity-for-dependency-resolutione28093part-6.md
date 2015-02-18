---
title: Custom Windows Workflow activity for dependency resolution–Part 6
categories : .Net
tags : WPF
date: 2010-10-01 13:10:00 +10:00
---

The [previous post][0] in this series provided a custom updatable generic type argument implementation for the InstanceResolver activity. This post will look at the the XAML designer support for this activity.

The designer support for the InstanceResolver intends to display only the number of dependency resolutions that are configured according to the ArgumentCount property. Each dependency resolution shown needs to provide editing functionality for the resolution type, resolution name and the name of the handler reference.![][1]![image][2]

The XAML for the designer defines the activity icon, display for each argument and the child activity to execute. Each of the arguments is bound to an attached property that defines whether that argument is visible to the designer.

    <sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.InstanceResolverDesigner&quot;
                          xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot;
                          xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
                          xmlns:s=&quot;clr-namespace:System;assembly=mscorlib&quot;
                          xmlns:sap=&quot;clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation&quot;
                          xmlns:sapv=&quot;clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation&quot;
                          xmlns:conv=&quot;clr-namespace:System.Activities.Presentation.Converters;assembly=System.Activities.Presentation&quot;
                          xmlns:sadm=&quot;clr-namespace:System.Activities.Presentation.Model;assembly=System.Activities.Presentation&quot;
                          xmlns:ComponentModel=&quot;clr-namespace:System.ComponentModel;assembly=WindowsBase&quot;
                          xmlns:ntw=&quot;clr-namespace:Neovolve.Toolkit.Workflow;assembly=Neovolve.Toolkit.Workflow&quot;
                          xmlns:ntwd=&quot;clr-namespace:Neovolve.Toolkit.Workflow.Design&quot;&gt;
        <sap:ActivityDesigner.Icon&gt;
            <DrawingBrush&gt; 
                <DrawingBrush.Drawing&gt;
                    <ImageDrawing&gt;
                        <ImageDrawing.Rect&gt;
                            <Rect Location=&quot;0,0&quot;
                                  Size=&quot;16,16&quot;&gt;
                            </Rect&gt;
                        </ImageDrawing.Rect&gt;
                        <ImageDrawing.ImageSource&gt;
                            <BitmapImage UriSource=&quot;brick.png&quot;&gt;</BitmapImage&gt;
                        </ImageDrawing.ImageSource&gt;
                    </ImageDrawing&gt;
                </DrawingBrush.Drawing&gt;
            </DrawingBrush&gt;
        </sap:ActivityDesigner.Icon&gt;
        <sap:ActivityDesigner.Resources&gt;
        <conv:ModelToObjectValueConverter x:Key=&quot;modelItemConverter&quot;
                                              x:Uid=&quot;sadm:ModelToObjectValueConverter_1&quot; /&gt;
            <conv:ArgumentToExpressionConverter x:Key=&quot;expressionConverter&quot; /&gt;
            <ObjectDataProvider MethodName=&quot;GetValues&quot;
                                ObjectType=&quot;{x:Type s:Enum}&quot;
                                x:Key=&quot;EnumSource&quot;&gt;
                <ObjectDataProvider.MethodParameters&gt;
                    <x:Type TypeName=&quot;ntw:GenericArgumentCount&quot; /&gt;
                </ObjectDataProvider.MethodParameters&gt;
            </ObjectDataProvider&gt;
            <DataTemplate x:Key=&quot;Collapsed&quot;&gt;
                <TextBlock HorizontalAlignment=&quot;Center&quot;
                           FontStyle=&quot;Italic&quot;
                           Foreground=&quot;Gray&quot;&gt;
                    Double-click to view
                </TextBlock&gt;
            </DataTemplate&gt;
            <DataTemplate x:Key=&quot;Expanded&quot;&gt;
                <Grid&gt;
                    <Grid.RowDefinitions&gt;
                        <RowDefinition Height=&quot;Auto&quot; /&gt;
                        <RowDefinition Height=&quot;*&quot; /&gt;
                    </Grid.RowDefinitions&gt;
            <StackPanel Orientation=&quot;Vertical&quot;&gt;
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;&gt;
                <TextBlock VerticalAlignment=&quot;Center&quot;&gt;Argument Count:</TextBlock&gt;
                <ComboBox x:Name=&quot;ArgumentCountList&quot;
                                  ItemsSource=&quot;{Binding Source={StaticResource EnumSource}}&quot;
                                  SelectedItem=&quot;{Binding Path=ModelItem.Arguments, Mode=TwoWay}&quot;/&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.ArgumentVisible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;false&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument1.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName1, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument1Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;false&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType1, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument1.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName1, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument2Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType2, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument2.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName2, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument3Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType3, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument3.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName3, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument4Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType4, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument4.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName4, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument5Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType5, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument5.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName5, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument6Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType6, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument6.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName6, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument7Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType7, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument7.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName7, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument8Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType8, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument8.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName8, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument9Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType9, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument9.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName9, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument10Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType10, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument10.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName10, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument11Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType11, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument11.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName11, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument12Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType12, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument12.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName12, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument13Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType13, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument13.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName13, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument14Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType14, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument14.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName14, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument15Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType15, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument15.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName15, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
              <StackPanel Orientation=&quot;Horizontal&quot;
                                Margin=&quot;2&quot;
                                Grid.Row=&quot;2&quot; Visibility=&quot;{Binding Path=ModelItem.Argument16Visible}&quot;&gt;
                <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;true&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType16, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                <TextBox Text=&quot;{Binding ModelItem.Body.Argument16.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
    
                <TextBlock VerticalAlignment=&quot;Center&quot;
                                   Margin=&quot;2&quot;&gt;
                            with name
                </TextBlock&gt;
    
                <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.ResolutionName16, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:String&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot;
                                                Margin=&quot;2&quot; /&gt;
              </StackPanel&gt;
    
            </StackPanel&gt;
            
                    <sap:WorkflowItemPresenter Item=&quot;{Binding ModelItem.Body.Handler}&quot;
                                               HintText=&quot;Drop activity&quot;
                                               Grid.Row=&quot;1&quot;
                                               Margin=&quot;6&quot; /&gt;
                </Grid&gt;
            </DataTemplate&gt;
            <Style x:Key=&quot;ExpandOrCollapsedStyle&quot;
                   TargetType=&quot;{x:Type ContentPresenter}&quot;&gt;
                <Setter Property=&quot;ContentTemplate&quot;
                        Value=&quot;{DynamicResource Collapsed}&quot; /&gt;
                <Style.Triggers&gt;
                    <DataTrigger Binding=&quot;{Binding Path=ShowExpanded}&quot;
                                 Value=&quot;true&quot;&gt;
                        <Setter Property=&quot;ContentTemplate&quot;
                                Value=&quot;{DynamicResource Expanded}&quot; /&gt;
                    </DataTrigger&gt;
                </Style.Triggers&gt;
            </Style&gt;
        </sap:ActivityDesigner.Resources&gt;
        <Grid&gt;
            <ContentPresenter Style=&quot;{DynamicResource ExpandOrCollapsedStyle}&quot;
                              Content=&quot;{Binding}&quot; /&gt;
        </Grid&gt;
    </sap:ActivityDesigner&gt;{% endhighlight %}

There is a lot of duplication in this XAML for each of the argument definitions and I am quite embarrassed by this poor implementation. It is a result of having next to zero WPF experience and needing to trade-off my desire for perfection with the demand for my time to work on other projects. I attempted a UserControl to reduce this duplication but hit many hurdles around binding the expression of the ResolutionName properties through to the ExpressionTextBox in the UserControl. Hopefully greater minds will be able to contribute a better solution for this part of the series.

The code behind this designer detects a new ModelItem being assigned and then attaches properties to it that are bound to in the XAML.

    namespace Neovolve.Toolkit.Workflow.Design.Presentation
    {
        using System;
        using System.Diagnostics;
        using Neovolve.Toolkit.Workflow.Activities;
    
        public partial class InstanceResolverDesigner
        {
            [DebuggerNonUserCode]
            public InstanceResolverDesigner()
            {
                InitializeComponent();
            }
    
            protected override void OnModelItemChanged(Object newItem)
            {
                InstanceResolverDesignerExtension.Attach(ModelItem);
    
                base.OnModelItemChanged(newItem);
            }
        }
    }{% endhighlight %}

The InstanceResolverDesignerExtension class creates attached properties to manage the InstanceResolver.ArgumentCount property and the set of properties that control the argument visibility state. 

    namespace Neovolve.Toolkit.Workflow.Design
    {
        using System;
        using System.Activities.Presentation;
        using System.Activities.Presentation.Model;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.Windows;
    
        public class InstanceResolverDesignerExtension
        {
            internal const String Arguments = &quot;Arguments&quot;;
    
            private const String ArgumentCount = &quot;ArgumentCount&quot;;
    
            private readonly Dictionary<String, AttachedProperty&gt; _attachedProperties = new Dictionary<String, AttachedProperty&gt;();
    
            private GenericArgumentCount _previousArguments;
    
            public static void Attach(ModelItem modelItem)
            {
                EditingContext editingContext = modelItem.GetEditingContext();
                InstanceResolverDesignerExtension designerExtension = editingContext.Services.GetService<InstanceResolverDesignerExtension&gt;();
    
                if (designerExtension == null)
                {
                    designerExtension = new InstanceResolverDesignerExtension();
    
                    editingContext.Services.Publish(designerExtension);
                }
    
                designerExtension.AttachToModelItem(modelItem);
            }
    
            private static AttachedProperty<Visibility&gt; AttachArgumentVisibleProperty(
                AttachedPropertiesService attachedPropertiesService, ModelItem modelItem, Int32 argumentNumber)
            {
                String propertyName;
    
                if (argumentNumber &gt; 0)
                {
                    propertyName = &quot;Argument&quot; + argumentNumber + &quot;Visible&quot;;
                }
                else
                {
                    propertyName = &quot;ArgumentVisible&quot;;
                }
    
                AttachedProperty<Visibility&gt; attachedProperty = new AttachedProperty<Visibility&gt;
                                                                {
                                                                    IsBrowsable = false, 
                                                                    Name = propertyName, 
                                                                    OwnerType = modelItem.ItemType, 
                                                                    Getter = delegate(ModelItem modelReference)
                                                                             {
                                                                                 Int32 argumentCount =
                                                                                     (Int32)modelReference.Properties[Arguments].ComputedValue;
    
                                                                                 if (argumentNumber == 0 && argumentCount == 1)
                                                                                 {
                                                                                     return Visibility.Visible;
                                                                                 }
    
                                                                                 if (argumentNumber != 0 && argumentCount &gt; 1 &&
                                                                                     argumentNumber <= argumentCount)
                                                                                 {
                                                                                     return Visibility.Visible;
                                                                                 }
    
                                                                                 return Visibility.Collapsed;
                                                                             }, 
                                                                    Setter = delegate(ModelItem modelReference, Visibility newValue)
                                                                             {
                                                                                 Debug.WriteLine(&quot;Visibility updated to &quot; + newValue);
                                                                             }
                                                                };
    
                attachedPropertiesService.AddProperty(attachedProperty);
    
                return attachedProperty;
            }
    
            private void AttachArgumentsProperty(ModelItem modelItem, AttachedPropertiesService attachedPropertiesService)
            {
                AttachedProperty<GenericArgumentCount&gt; argumentsProperty = new AttachedProperty<GenericArgumentCount&gt;
                                                                           {
                                                                               Name = Arguments, 
                                                                               IsBrowsable = true, 
                                                                               OwnerType = modelItem.ItemType, 
                                                                               Getter = delegate(ModelItem modelReference)
                                                                                        {
                                                                                            return
                                                                                                (GenericArgumentCount)
                                                                                                modelReference.Properties[ArgumentCount].ComputedValue;
                                                                                        }, 
                                                                               Setter = delegate(ModelItem modelReference, GenericArgumentCount newValue)
                                                                                        {
                                                                                            _previousArguments =
                                                                                                (GenericArgumentCount)
                                                                                                modelReference.Properties[ArgumentCount].ComputedValue;
    
                                                                                            modelReference.Properties[ArgumentCount].ComputedValue =
                                                                                                newValue;
    
                                                                                            // Update the activity to use InstanceResolver with the correct number of arguments
                                                                                            UpgradeInstanceResolverType(modelReference);
                                                                                        }
                                                                           };
    
                attachedPropertiesService.AddProperty(argumentsProperty);
            }
    
            private void AttachToModelItem(ModelItem modelItem)
            {
                // Get the number of arguments from the model
                GenericArgumentCount argumentCount = (GenericArgumentCount)modelItem.Properties[ArgumentCount].ComputedValue;
                EditingContext editingContext = modelItem.GetEditingContext();
                AttachedPropertiesService attachedPropertiesService = editingContext.Services.GetService<AttachedPropertiesService&gt;();
    
                // Store the previous argument count value to track the changes to the property
                _previousArguments = argumentCount;
    
                // Attach updatable type arguments
                InstanceResolverTypeUpdater.AttachUpdatableArgumentTypes(modelItem, (Int32)argumentCount);
    
                AttachArgumentsProperty(modelItem, attachedPropertiesService);
    
                // Start from the second argument because there must always be at least one instance resolution per InstaceResolver
                for (Int32 index = 0; index <= 16; index++)
                {
                    // Create an attached property that calculates the designer visibility for a stack panel
                    AttachedProperty<Visibility&gt; argumentVisibleProperty = AttachArgumentVisibleProperty(attachedPropertiesService, modelItem, index);
    
                    _attachedProperties[argumentVisibleProperty.Name] = argumentVisibleProperty;
                }
            }
    
            private void UpgradeInstanceResolverType(ModelItem modelItem)
            {
                Type[] originalGenericTypes = modelItem.ItemType.GetGenericArguments();
                Type[] genericTypes = new Type[16];
    
                originalGenericTypes.CopyTo(genericTypes, 0);
    
                if (originalGenericTypes.Length < genericTypes.Length)
                {
                    Type defaultGenericType = typeof(Object);
    
                    // This type is being upgraded from InstanceResolver to InstanceResolver
                    // Initialize the generic types with Object
                    for (Int32 index = originalGenericTypes.Length; index < genericTypes.Length; index++)
                    {
                        genericTypes[index] = defaultGenericType;
                    }
                }
    
                Type newType = RegisterMetadata.InstanceResolverT16GenericType.MakeGenericType(genericTypes);
    
                InstanceResolverTypeUpdater.UpdateModelType(modelItem, newType, _previousArguments);
            }
        }
    }{% endhighlight %}

An attached property is used for the ArgumentCount property in order to track changes to the property. Updates to this property then cause an update to the ModelItem.There did not seem to be any other way to detect changes to this property from the designer code when binding directly to the property on the activity.

There were a few other designer quirks discovered throughout this exercise. 


      1. The binding to the visibility attached properties did not correctly update in the designer when the values were changed. The property is actually only a Getter in its functionality as it is a calculation based on the current argument number against the total number of displayed arguments. The way to get the binding to be updatable was to provide a Setter that did nothing.

    
      1. Attached properties marked as IsBrowsable = false are not available via the ModelItem.Properties collection. The solution here was to cache these attached properties against the extension (service) stored against the services of the associated EditingContext.

    
      1. Attached properties cannot be removed once they are attached. This affected the desired outcome for the property grid experience.

    
      1. Attached properties cannot have custom attributes assigned to them so there is no support for Description or Category values in the property grid.

    
The designer support using this implementation is usable but far from perfect. The biggest issue is synchronisation between the ModelItem on the designer and the property grid. The property grid can get a little out of sync especially with changes to the Arguments property. The problem here is the ability to refresh/update the contents of the property grid when certain events occur on the ModelItem in the designer. Interacting with the activity on the design surface directly seems to provide the most reliable result.

This post has covered the designer support for the InstanceResolver activity and provided information on some gotchas for working with activity designers. The posts in this series have covered all aspects of implementing a custom activity for resolving dependencies within WF4 workflows.

[0]: /post/2010/09/30/Custom-Windows-Workflow-activity-for-dependency-resolutione28093Part-5.aspx
[1]: //blogfiles/image_36.png
[2]: //blogfiles/image_37.png
