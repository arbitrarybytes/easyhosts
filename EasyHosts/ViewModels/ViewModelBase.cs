using Microsoft.Practices.Prism.Mvvm;
using System.Xml.Serialization;

namespace EasyHosts.ViewModels
{
    public class ViewModelBase : BindableBase
    {
        private const string DirtyChar = " *";

        /// <summary>
        /// Gets or sets the name of the view model
        /// </summary>
        [XmlIgnore()]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// Gets or sets if the view model has unsaved changes.
        /// </summary>
        [XmlIgnore()]
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;
                OnPropertyChanged("IsDirty");
                if (CanDirty && !string.IsNullOrWhiteSpace(Name))
                {
                    if (_isDirty && !Name.EndsWith(DirtyChar))
                        Name = Name + DirtyChar;

                    if (!_isDirty && Name.EndsWith(DirtyChar))
                        Name = Name.Substring(0, Name.Length - DirtyChar.Length);
                }
            }
        }

        /// <summary>
        /// Gets or sets if the view model change tracking is enabled 
        /// and if enabled updates the Name property accordingly.
        /// </summary>
        [XmlIgnore()]
        public bool CanDirty
        {
            get { return _canDirty; }
            set
            {
                _canDirty = value;
                OnPropertyChanged("CanDirty");
            }
        }

        /// <summary>
        /// Gets or sets the message to display.
        /// </summary>
        [XmlIgnore()]
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        /// <summary>
        /// Gets or sets the details relating to the Message, optional.
        /// </summary>
        [XmlIgnore()]
        public string MessageDetails
        {
            get
            {
                return string.IsNullOrWhiteSpace(_messageDetails) ? string.Empty : "\n" + _messageDetails;
            }
            set
            {
                _messageDetails = value;
                OnPropertyChanged("MessageDetails");
            }
        }

        /// <summary>
        /// Gets or sets the type of message, defaults to None.
        /// </summary>
        [XmlIgnore()]
        public MessageType MessageType
        {
            get { return _messageType; }
            set
            {
                _messageType = value;
                OnPropertyChanged("MessageType");
            }
        }

        /// <summary>
        /// Clears the message, message details and resets the MessageType to None.
        /// </summary>
        public void ClearMessage()
        {
            MessageType = MessageType.None;
            Message = string.Empty;
            MessageDetails = string.Empty;
            ShowMessage = false;
        }

        /// <summary>
        /// Sets the message and optionally the message type and details.
        /// </summary>
        /// <param name="message">the message to display</param>
        /// <param name="messageType">the type of message</param>
        /// <param name="details">the details relating to the message</param>
        /// <param name="detailsPrefix">the prefix to use for the details message</param>
        public void SetMessage(string message, MessageType messageType = MessageType.None, string details = "", string detailsPrefix = "Details: ")
        {
            Message = message;
            MessageType = messageType;
            MessageDetails = !string.IsNullOrWhiteSpace(details) ? detailsPrefix + details : details;
            ShowMessage = true;
        }

        [XmlIgnore()]
        public bool ShowMessage
        {
            get { return _showMessage; }
            set
            {
                _showMessage = value;
                OnPropertyChanged("ShowMessage");
            }
        }

        [XmlIgnore()]
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        #region :: Fields ::

        private string _name;
        private bool _isDirty;
        private bool _canDirty;
        private string _message;
        private string _messageDetails;
        private MessageType _messageType = MessageType.None;
        private bool _showMessage;
        private bool _isBusy;

        #endregion
    }
}
