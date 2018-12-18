using Hello.Attributes;
using Hello.Contracts;
using Hello.Exceptions;
using Hello.Extensions;
using Hello.Model;
using Hello.Model.Enums;
using Hello.Services;
using Hello.Wpf.Apps;
using Hello.Wpf.Converters;
using Hello.Wpf.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

#region AssemblyAttributes

[assembly: AssemblyTitle("Hello")]
[assembly: AssemblyDescription("Program to display a 'Hello, world!' message")]
[assembly: AssemblyCompany("The Hello, world! community")]
[assembly: AssemblyProduct("Hello")]
[assembly: AssemblyCopyright("Copyright © 2018, the 'Hello, world!' community")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]

#endregion

namespace Hello
{
    namespace Contracts
    {
        public interface IHelloWorldService
        {
            Task<string> GetHelloWorldText();
            Task<IValueConverter> GetHelloWorldStringToStringConverter();
            Task<IValueConverter> GetHelloWorldModelToStringConverter();
        }

        public interface IHaveReadOnlyDisplayName
        {
            string DisplayName { get; }
        }
    }

    namespace Model
    {
        namespace Enums
        {
            public enum TextId
            {
                HelloWorldTextId,
            }

            public enum HelloWorldExceptionReturnCode
            {
                Success = 0,
                PropertyNameRetrievalTaskCancelled = 1,
                NoWindowClass = 2,
            }
        }

        public abstract class PropertyChangeBase : INotifyPropertyChanged
        {
#pragma warning disable CS0067 // Handler used via reflection in SetProperty Extension
            public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067
        }

        public class HelloWorldModel : PropertyChangeBase, IComparable<HelloWorldModel>, IComparable, ICloneable, IHaveReadOnlyDisplayName
        {
            private string displayName;

            public HelloWorldModel(string displayName)
            {
                DisplayName = displayName;
            }

            public string DisplayName
            {
                get => displayName;

                private set => this.SetProperty(ref displayName, value, preAction: () =>
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(DisplayName), "Cannot set to null");
                    }
                });
            }

            public override string ToString()
            {
                return DisplayName;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is HelloWorldModel other)
                {
                    return DisplayName.Equals(other.DisplayName);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return DisplayName?.GetHashCode() ?? 0;
            }

            public object Clone()
            {
                return new HelloWorldModel(DisplayName);
            }

            public static bool operator ==(HelloWorldModel left, HelloWorldModel right)
            {
                return left is null ? right is null : EqualityComparer<HelloWorldModel>.Default.Equals(left, right);
            }

            public static bool operator !=(HelloWorldModel left, HelloWorldModel right)
            {
                return !(left == right);
            }

            public int CompareTo(HelloWorldModel other)
            {
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                return other is null ? 1 : string.Compare(displayName, other.displayName, StringComparison.CurrentCulture);
            }

            public int CompareTo(object obj)
            {
                if (obj is null)
                {
                    return 1;
                }

                if (ReferenceEquals(this, obj))
                {
                    return 0;
                }

                return obj is HelloWorldModel other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(HelloWorldModel)}");
            }

            public static bool operator <(HelloWorldModel left, HelloWorldModel right)
            {
                return Comparer<HelloWorldModel>.Default.Compare(left, right) < 0;
            }

            public static bool operator >(HelloWorldModel left, HelloWorldModel right)
            {
                return Comparer<HelloWorldModel>.Default.Compare(left, right) > 0;
            }

            public static bool operator <=(HelloWorldModel left, HelloWorldModel right)
            {
                return Comparer<HelloWorldModel>.Default.Compare(left, right) <= 0;
            }

            public static bool operator >=(HelloWorldModel left, HelloWorldModel right)
            {
                return Comparer<HelloWorldModel>.Default.Compare(left, right) >= 0;
            }

            public static explicit operator string(HelloWorldModel helloWorldModel)
            {
                return helloWorldModel.DisplayName;
            }

            public static explicit operator HelloWorldModel(string value)
            {
                return new HelloWorldModel(value);
            }
        }
    }

    namespace Attributes
    {
        [AttributeUsage(AttributeTargets.Field)]
        public sealed class HelloWorldAttribute : Attribute { }
    }

    namespace ProgramEntry
    {
        internal class Program
        {
            [STAThread]
            private static void Main()
            {
                var helloWorldApp = new HelloWorldApp();
                var exitCode = helloWorldApp.Run();
                Environment.ExitCode = exitCode;
            }
        }
    }

    namespace Wpf
    {
        namespace Apps
        {
            public class HelloWorldApp : Application, INotifyPropertyChanged
            {
                public static readonly IHelloWorldService HelloWorldService = new HelloWorldService();
                private HelloWorldModel helloWorldModel;

#pragma warning disable CS0067 // Handler used via reflection in SetProperty Extension
                public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

                public HelloWorldModel HelloWorldModel
                {
                    get => helloWorldModel;

                    set => this.SetProperty(ref helloWorldModel, value, preAction: () =>
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException
                            (
                                nameof(HelloWorldModel),
                                "Cannot set to null"
                            );
                        }
                    });
                }

                protected override async void OnStartup(StartupEventArgs startupEventArgs)
                {
                    ShutdownMode = ShutdownMode.OnLastWindowClose;
                    base.OnStartup(startupEventArgs);

                    DispatcherUnhandledException += (sender, dispatcherUnhandledExceptionEventArgs) =>
                    {
                        MessageBox.Show
                        (
                            $"{dispatcherUnhandledExceptionEventArgs.Exception.Message}",
                            "The program encountered an unhandled exception",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error,
                            MessageBoxResult.OK
                        );
                    };

                    var helloWindowClassName = typeof(HelloWindow).FullName;

                    if (string.IsNullOrWhiteSpace(helloWindowClassName))
                    {
                        throw new HelloWorldException(null, HelloWorldExceptionReturnCode.NoWindowClass, null);
                    }

                    var helloWindow = (HelloWindow)Activator.CreateInstance
                    (
                        AppDomain.CurrentDomain,
                        Assembly.GetExecutingAssembly().GetName().Name,
                        helloWindowClassName
                    ).Unwrap();

                    GetType().GetProperty(nameof(HelloWorldModel))?
                        .SetValue(this, (HelloWorldModel)await HelloWorldService.GetHelloWorldText());

                    var binding = new Binding(nameof(HelloWorldModel))
                    {
                        Source = this,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Converter = await HelloWorldService.GetHelloWorldModelToStringConverter(),
                        ConverterCulture = CultureInfo.CurrentCulture,
                    };

                    var helloWindowDependencyPropertyField = typeof(HelloWindow)
                        .GetFields()
                        .AsParallel()
                        .Single
                        (
                            fieldInfo => fieldInfo.GetCustomAttribute<HelloWorldAttribute>(false) != null &&
                                         typeof(DependencyProperty).IsAssignableFrom(fieldInfo.FieldType)
                        );

                    var helloWindowDependencyProperty = (DependencyProperty)helloWindowDependencyPropertyField.GetValue(null);

                    typeof(HelloWindow)
                        .GetMethods()
                        .AsParallel()
                        .Single
                        (
                            m =>
                            {
                                var name = m.Name;
                                var parameters = m.GetParameters();

                                return name == nameof(HelloWindow.SetBinding) &&
                                          parameters.Length == 2 &&
                                          parameters[1].ParameterType == typeof(BindingBase);
                            }
                        )
                        ?.Invoke(helloWindow, new object[] { helloWindowDependencyProperty, binding });

                    typeof(HelloWindow)
                        .GetMethod(nameof(HelloWindow.ShowDialog))?
                        .Invoke(helloWindow, null);
                }
            }
        }

        namespace Views
        {
            public class HelloWindow : Window
            {
                private readonly TextBlock textBlock;

                [HelloWorld]
                public static DependencyProperty HelloWorldTextProperty = DependencyProperty.Register
                (
                    nameof(HelloWorldText),
                    typeof(string),
                    typeof(HelloWindow),
                    new PropertyMetadata(default(string), HelloWorldTextChanged)
                );

                public HelloWindow()
                {
                    DependencyPropertyDescriptor GetDependencyProperty
                    (
                        DependencyObject dependencyObject,
                        string propertyName
                    )
                    {
                        return DependencyPropertyDescriptor.FromName
                        (
                            propertyName,
                            dependencyObject.GetType(),
                            dependencyObject.GetType(),
                            true
                        );
                    }

                    var viewBoxMargin = new Thickness(10, 0, 10, 3);
                    var viewBox = new Viewbox();

                    GetDependencyProperty(viewBox, nameof(viewBox.Stretch)).SetValue(viewBox, Stretch.Uniform);
                    GetDependencyProperty(viewBox, nameof(viewBox.Margin)).SetValue(viewBox, viewBoxMargin);

                    textBlock = new TextBlock();
                    GetDependencyProperty(textBlock, nameof(textBlock.DataContext)).SetValue(textBlock, this);

                    Content = viewBox;
                    viewBox.Child = textBlock;
                    var helloWorldPropertyName = default(string);

                    using (var tokenSource = new CancellationTokenSource())
                    {
                        try
                        {
                            var helloWorldPropertyNameRetrievalTask = Task.Run(() =>
                            {
                                helloWorldPropertyName = nameof(HelloWorldText);
                                tokenSource.Token.ThrowIfCancellationRequested();
                            }, tokenSource.Token);

                            var timeout = new TimeSpan(0, 0, 10);

                            if (!helloWorldPropertyNameRetrievalTask.Wait((int)Math.Round(timeout.TotalMilliseconds),
                                tokenSource.Token))
                            {
                                throw new TimeoutException(
                                    $"Could not retrieve {nameof(helloWorldPropertyName)} with in {timeout:g} seconds.");
                            }
                        }
                        catch (Exception exception) when (exception is OperationCanceledException)
                        {
                            throw new HelloWorldException
                            (
                                exception,
                                HelloWorldExceptionReturnCode.PropertyNameRetrievalTaskCancelled,
                                nameof(helloWorldPropertyName),
                                nameof(HelloWorldText)
                            );
                        }
                    }

                    Loaded += async (sender, routedEventArgs) =>
                    {
                        var binding = new Binding(helloWorldPropertyName)
                        {
                            Source = this,
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            Converter = await HelloWorldApp.HelloWorldService.GetHelloWorldStringToStringConverter(),
                            ConverterCulture = CultureInfo.CurrentCulture,
                        };

                        textBlock.SetBinding(TextBlock.TextProperty, binding);
                    };
                }

                public string HelloWorldText
                {
                    get => (string)GetValue(HelloWorldTextProperty);
                    set => SetValue(HelloWorldTextProperty, value);
                }

                public static void HelloWorldTextChanged
                (
                    DependencyObject helloWorldDependencyObject,
                    DependencyPropertyChangedEventArgs helloWorldPropertyChangedEventArgs
                )
                {
                    var helloWindowInstance = (HelloWindow)helloWorldDependencyObject;
                    var oldValue = (string)helloWorldPropertyChangedEventArgs.OldValue;
                    var newValue = (string)helloWorldPropertyChangedEventArgs.NewValue;
                    var bindingExpression = helloWindowInstance.textBlock.GetBindingExpression(TextBlock.TextProperty);

                    if (newValue != oldValue && bindingExpression == null)
                    {
                        helloWindowInstance.textBlock.Text = newValue ?? throw new ArgumentNullException
                        (
                           nameof(HelloWorldText),
                           $@"Please use {nameof(String)}.{nameof(string.Empty)} to display an empty Window"
                        );
                    }
                }

                public override string ToString()
                {
                    return HelloWorldText;
                }
            }
        }

        namespace Converters
        {
            public class HelloWorldStringToStringConverter : IValueConverter
            {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    var result = System.Convert.ChangeType(value, targetType, culture);

                    if (result == null && value != null)
                    {
                        throw new ArgumentException($"Cannot convert {value} to {targetType.FullName}");
                    }

                    if (parameter != null)
                    {
                        throw new NotSupportedException("This converter does not accept parameters");
                    }

                    return result;
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    return Convert(value, targetType, parameter, culture);
                }
            }

            public class HelloWorldModelToStringConverter : IValueConverter
            {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    if (!(value is IHaveReadOnlyDisplayName displayNameProvider))
                    {
                        throw new NotSupportedException($"Only {nameof(IHaveReadOnlyDisplayName)} is supported");
                    }

                    if (parameter != null)
                    {
                        throw new NotSupportedException("This converter does not accept parameters");
                    }

                    if (!targetType.IsAssignableFrom(typeof(string)))
                    {
                        throw new NotSupportedException($"Can only convert {nameof(IHaveReadOnlyDisplayName)} to {nameof(String)}");
                    }

                    return displayNameProvider.DisplayName.ToString(culture);
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    if (!(value is IConvertible convertible))
                    {
                        throw new NotSupportedException($"Only {nameof(IConvertible)} is supported");
                    }

                    if (parameter != null)
                    {
                        throw new NotSupportedException("This converter does not accept parameters");
                    }

                    if (!targetType.IsAssignableFrom(typeof(IHaveReadOnlyDisplayName)))
                    {
                        throw new NotSupportedException($"Can only convert {nameof(IConvertible)} to {nameof(IHaveReadOnlyDisplayName)}");
                    }

                    IHaveReadOnlyDisplayName result = new HelloWorldModel(convertible.ToString(culture));
                    return result;
                }
            }
        }
    }

    namespace Exceptions
    {
        public class HelloWorldException : Exception
        {
            private static readonly IDictionary<HelloWorldExceptionReturnCode, string> errorDictionary = new Dictionary<HelloWorldExceptionReturnCode, string>
            {
                {
                    HelloWorldExceptionReturnCode.Success,
                    new Win32Exception(0).Message
                },
                {
                    HelloWorldExceptionReturnCode.PropertyNameRetrievalTaskCancelled,
                    "The retrieval of {0} which should be \"{1}\" was cancelled"
                },
                {
                    HelloWorldExceptionReturnCode.NoWindowClass,
                    "Window has no class name"
                },
            };

            public HelloWorldException
            (
                Exception innerException,
                HelloWorldExceptionReturnCode errorCode,
                params object[] parameters
            ) : base(GetMessage(errorCode, parameters), innerException)
            {
                ErrorCode = errorCode;
            }

            public static string GetMessage(HelloWorldExceptionReturnCode errorCode, object[] parameters)
            {
                var formatString = errorDictionary[errorCode];
                return string.Format(CultureInfo.CurrentCulture, formatString, parameters);
            }

            public HelloWorldExceptionReturnCode ErrorCode { get; }
        }
    }

    namespace Services
    {
        public class HelloWorldService : IHelloWorldService
        {
            private static readonly string helloWorldText;
            private static readonly IReadOnlyDictionary<TextId, string> textDictionary;
            private static readonly IValueConverter helloWorldConverter;
            private static readonly IValueConverter helloWorldModelToStringConverter;

            static HelloWorldService()
            {
                helloWorldText = "Hello, World!";
                var concurrentDictionary = new ConcurrentDictionary<TextId, string>();
                ((IDictionary<TextId, string>)concurrentDictionary).Add(TextId.HelloWorldTextId, helloWorldText);
                textDictionary = concurrentDictionary;
                helloWorldConverter = new HelloWorldStringToStringConverter();
                helloWorldModelToStringConverter=new HelloWorldModelToStringConverter();
            }

            public async Task<string> GetHelloWorldText()
            {
                string result = null;

                await Task.Run(() =>
                {
                    if (!textDictionary.ContainsKey(TextId.HelloWorldTextId))
                    {
                        throw new KeyNotFoundException($"Text \"{helloWorldText}\" is not present in text dictionary");
                    }

                    result = textDictionary[TextId.HelloWorldTextId];
                });

                return result;
            }

            public async Task<IValueConverter> GetHelloWorldStringToStringConverter()
            {
                IValueConverter result = null;
                await Task.Run(() => result = helloWorldConverter);
                return result;
            }

            public async Task<IValueConverter> GetHelloWorldModelToStringConverter()
            {
                IValueConverter result = null;
                await Task.Run(() => result = helloWorldModelToStringConverter);
                return result;
            }
        }
    }

    namespace Extensions
    {
        public static class Extensions
        {
            public static void SetProperty<TProperty>
            (
                this INotifyPropertyChanged instance,
                ref TProperty propertyBackingField,
                TProperty value,
                Action preAction = null,
                Action postAction = null,
                [CallerMemberName] string propertyName = null
            )
            {
                if
                (
                    !ReferenceEquals(propertyBackingField, value) ||
                    propertyBackingField != null &&
                    (
                        propertyBackingField.GetHashCode() != value.GetHashCode() ||
                        !propertyBackingField.Equals(value)
                    )
                )
                {
                    var propertyChangedEventHandler = (PropertyChangedEventHandler)instance
                        .GetType()
                        .GetField
                        (
                            nameof(instance.PropertyChanged),
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )?
                        .GetValue(instance);

                    preAction?.Invoke();
                    propertyBackingField = value;
                    propertyChangedEventHandler?.Invoke(instance, new PropertyChangedEventArgs(propertyName));
                    postAction?.Invoke();
                }
            }
        }
    }
}
