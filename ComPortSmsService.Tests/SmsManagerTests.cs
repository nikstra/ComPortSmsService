using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;

namespace ComPortSmsService.Tests
{
    public class SmsManagerTests
    {
        private ISerialPort _serialPort;

        [TestFixture, SetCulture("en-US")]
        public class TheOpenPortMethod : SmsManagerTests
        {

            [Test]
            public void SuccessfullyOpens()
            {
                _serialPort.IsOpen.Returns(true);
                var manager = new SmsManager(_serialPort);

                manager.OpenPort("COM2", 9600, 8, 300, 300);

                Assert.IsTrue(manager.PortIsOpen);
            }

            [Test]
            public void ThrowsExceptionOnAccessDenied()
            {
                _serialPort.When(call => call.Open()).Do(callback => { throw new UnauthorizedAccessException(); });
                var manager = new SmsManager(_serialPort);

                var ex = Assert.Throws<UnauthorizedAccessException>(() =>
                    manager.OpenPort("COM2", 9600, 8, 300, 300));

                Assert.That(ex.Message, Is.EqualTo("Attempted to perform an unauthorized operation."));
            }

            [Test]
            public void ThrowsExceptionOnArgumentOutOfRange()
            {
                var manager = new SmsManager(_serialPort);

                Assert.That(() => manager.OpenPort("COM2", 9600, 65535, 300, 300),
                    Throws.Exception.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.EqualTo("Specified argument was out of the range of valid values."));
            }

            [Test]
            public void ThrowsExceptionOnInvalidPortName()
            {
                var manager = new SmsManager(_serialPort);

                string portName = "LPT1";
                //string portName = "COM1";

                Assert.That(() => manager.OpenPort(portName, 9600, 8, 300, 300),
                    Throws.Exception.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Value does not fall within the expected range."));
            }

            [Test]
            public void ThrowsExceptionOnInvalidPortState()
            {
                _serialPort.When(call => call.Open()).Do(callback => { throw new IOException(); });
                var manager = new SmsManager(_serialPort);

                Assert.That(() => manager.OpenPort("COM2", 9600, 8, 300, 300),
                    Throws.Exception.TypeOf<IOException>()
                    .With.Message.EqualTo("I/O error occurred."));
            }

            [Test]
            public void ThrowsExceptionOnPortAlreadyOpen()
            {
                _serialPort.When(call => call.Open()).Do(callback => { throw new InvalidOperationException(); });
                var manager = new SmsManager(_serialPort);

                Assert.That(() => manager.OpenPort("COM2", 9600, 8, 300, 300),
                    Throws.Exception.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Operation is not valid due to the current state of the object."));
            }

        }

        [TestFixture, SetCulture("en-US")]
        public class TheClosePortMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyCloses()
            {
                _serialPort.IsOpen.Returns(false);
                var manager = new SmsManager(_serialPort);

                manager.ClosePort();

                Assert.IsFalse(manager.PortIsOpen);
            }

            [Test]
            public void ThrowsExceptionOnInvalidPortState()
            {
                _serialPort.When(call => call.Close()).Do(callback => { throw new IOException(); });
                var manager = new SmsManager(_serialPort);

                Assert.That(() => manager.ClosePort(),
                    Throws.Exception.TypeOf<IOException>()
                    .With.Message.EqualTo("I/O error occurred."));
            }

        }

        [TestFixture]
        public class TheExecCommandMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyExecutes()
            {
                // https://stackoverflow.com/questions/7582087/how-to-mock-serialdatareceivedeventargs
                var constructor = typeof(SerialDataReceivedEventArgs).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SerialData) }, null);
                var eventArgs = (SerialDataReceivedEventArgs)constructor.Invoke(new object[] { SerialData.Chars });

                //_serialPort.When(sp => sp.ReadExisting().Returns("\r\nOK\r\n")).Do(sp => _serialPort.DataReceived += Raise.Event<SerialDataReceivedEventHandler>(eventArgs));
                _serialPort.When(sp => sp.Write(Arg.Any<string>())).Do(sp => _serialPort.DataReceived += Raise.Event<SerialDataReceivedEventHandler>(eventArgs));

                //_serialPort.When(call => call.Write(Arg.Any<string>())); //.Do(callback => { throw new IOException(); });
                _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                var manager = new SmsManager(_serialPort);

                manager.OpenPort("COM2", 9600, 8, 300, 300);
                //_serialPort.DataReceived += Raise.Event<SerialDataReceivedEventHandler>(eventArgs);
                var result = manager.ExecCommand(_serialPort, "AT", 300, "No phone connected");
                manager.ClosePort();

                Assert.AreEqual(result, "\r\nOK\r\n");
            }

            // Commented because AxoCover does not respect Ignore and Explicit attributes.
            //[Test, Explicit("Integration test. Only valid if a modem/phone is connected.")]
            public void TestPhysicalPort()
            {
                var serialPort = new SystemSerialPort();
                var manager = new SmsManager(serialPort);

                manager.OpenPort("COM9", 9600, 8, 300, 300);
                var result = manager.ExecCommand(serialPort, "AT", 300, "No phone connected");
                manager.ClosePort();

                // Since ATE1 is the default the AT command is echoed back.
                Assert.AreEqual("AT\r\r\nOK\r\n", result);
            }
        }

        [TestFixture, SetCulture("en-US")]
        public class TheReadResponseMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyReadsResponse()
            {
                _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                var manager = new SmsManager(_serialPort);
                manager._receiveNow.Set();

                var result = manager.ReadResponse(_serialPort, 300);

                Assert.AreEqual("\r\nOK\r\n", result);
            }

            [Test]
            public void ThrowsExeptionWhenPortIsNotOpen()
            {
                _serialPort.When(sp => sp.ReadExisting()).Do(sp => { throw new InvalidOperationException(); });
                var manager = new SmsManager(_serialPort);
                manager._receiveNow.Set();

                Assert.That(() => manager.ReadResponse(_serialPort, 300),
                    Throws.Exception.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Operation is not valid due to the current state of the object."));
            }

            [Test]
            public void ThrowsExeptionWhenResponseIsIncomplete()
            {
                _serialPort.ReadExisting().Returns("Lorem Ipsum");
                var manager = new SmsManager(_serialPort);
                manager._receiveNow.Set();

                Assert.That(() => manager.ReadResponse(_serialPort, 300),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("Response received is incomplete."));
            }

            [Test]
            public void ThrowsExeptionWhenNoDataIsReceived()
            {
                var manager = new SmsManager(_serialPort);

                Assert.That(() => manager.ReadResponse(_serialPort, 300),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No data received from phone."));
            }
        }

        [TestFixture]
        public class TheCountSMSmessagesMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyCountsMessages()
            {
                // READ THIS: http://tech.findmypast.com/dont-mock-what-you-dont-own/
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CPMS?\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("+CPMS: \"SM\",4,20,\"SM\",0,20,\"ME\",186,1000\r\n\r\nOK\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                int count = manager.CountSMSmessages(_serialPort);

                // Assert
                Assert.AreEqual(4, count);
                // Note that CountSMSmessages() ignores messages not stored on SIM card.
            }

            [Test]
            public void ThrowsExceptionWhenConnectionFails()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.CountSMSmessages(_serialPort),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSetTextMode()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.CountSMSmessages(_serialPort),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToCountMessages()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CPMS?\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.CountSMSmessages(_serialPort),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }
        }

        [TestFixture, SetCulture("en-US")]
        public class TheReadSMSMethod : SmsManagerTests
        {
            // ReadAll
            //  "AT+CMGL=\"ALL\""
            // ReadUnRead
            //  "AT+CMGL=\"REC UNREAD\""
            // ReadStoreSent
            //  "AT+CMGL=\"STO SENT\""
            // ReadStoreUnSent
            //  "AT+CMGL=\"STO UNSENT\""

            [Test]
            public void SuccessfullyReturnsAllMessages()
            {
                // READ THIS: http://tech.findmypast.com/dont-mock-what-you-dont-own/
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CSCS=\"PCCP437\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CPMS=\"SM\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGL=\"ALL\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns(
@"+CMGL: 1,""REC UNREAD"",""+31628870634"",,""11/01/09,10:26:26+04""
This is text message 1
+CMGL: 2,""REC UNREAD"",""+31628870634"",,""11/01/09,10:26:49+04""
This is text message 2
OK
"
                    );
                    manager._receiveNow.Set();
                });

                // Act
                ShortMessageCollection result = manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\"");

                // Assert
                Assert.AreEqual(2, result.Count);
                StringAssert.Contains("This is text message 2", result[1].Message);
            }

            [Test]
            public void ThrowsExceptionWhenConnectionFails()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\""),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSetTextMode()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\""),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSelectCharacterSet()
            {
                // TODO: Fix test so that it tests the right thing.
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CSCS=\"PCCP437\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\""),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSelectMessageStorage()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CSCS=\"PCCP437\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CPMS=\"SM\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\""),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToListMessages()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CSCS=\"PCCP437\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CPMS=\"SM\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGL=\"ALL\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.ReadSMS(_serialPort, "AT+CMGL=\"ALL\""),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }
        }

        [TestFixture, SetCulture("en-US")]
        public class TheParseMessagesMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyParsesMessages()
            {
                // Arrange
                var messages =
@"+CMGL: 1,""REC UNREAD"",""+31628870634"",,""11/01/09,10:26:26+04""
This is text message 1
+CMGL: 2,""REC UNREAD"",""+31628870634"",,""11/01/09,10:26:49+04""
This is text message 2
OK
";
                var manager = new SmsManager(_serialPort);

                // Act
                ShortMessageCollection result = manager.ParseMessages(messages);

                // Assert
                Assert.AreEqual(2, result.Count);
                StringAssert.Contains("This is text message 2", result[1].Message);
            }

            [Test]
            public void ThrowsExceptionWhenArgumentIsNull()
            {
                // Arrange
                string messages = null;
                var manager = new SmsManager(_serialPort);

                // Act
                // Assert
                Assert.That(() => manager.ParseMessages(messages),
                    Throws.Exception.TypeOf<ArgumentNullException>()
                    .With.Message.StartsWith("Value cannot be null."));
            }
        }

        [TestFixture, SetCulture("en-US")]
        public class TheSendMsgMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullySendsAMessage()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGS=\"+31628870634\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "Hello World!" + (char)0x1A + "\r\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                bool result = manager.sendMsg(_serialPort, "+31628870634", "Hello World!");

                // Assert
                Assert.AreEqual(true, result);
            }

            [Test]
            public void ThrowsExceptionWhenConnectionFails()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.sendMsg(_serialPort, "+31628870634", "Hello World!"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSetTextMode()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.sendMsg(_serialPort, "+31628870634", "Hello World!"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenPhonenumberIsIncorrect()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGS=\"+31628870634\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.sendMsg(_serialPort, "+31628870634", "Hello World!"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }
            [Test]
            public void ThrowsExceptionWhenFailingToSendMessage()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGS=\"+31628870634\"\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "Hello World!" + (char)0x1A + "\r\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.sendMsg(_serialPort, "+31628870634", "Hello World!"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }
        }

        [TestFixture, SetCulture("en-US")]
        public class TheDeleteMsgMethod : SmsManagerTests
        {
            [Test]
            public void SuccessfullyDeletesReadMessages()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGD=1,3\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                bool result = manager.DeleteMsg(_serialPort, "AT+CMGD=1,3");

                // Assert
                Assert.AreEqual(true, result);
            }

            [Test]
            public void ThrowsExceptionWhenConnectionFails()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.DeleteMsg(_serialPort, "AT+CMGD=1,3"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToSetTextMode()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.DeleteMsg(_serialPort, "AT+CMGD=1,3"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }

            [Test]
            public void ThrowsExceptionWhenFailingToDeleteMessages()
            {
                // Arrange
                var manager = new SmsManager(_serialPort);
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGF=1\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nOK\r\n");
                    manager._receiveNow.Set();
                });
                _serialPort.When(sp => sp.Write(Arg.Is<string>(a => a == "AT+CMGD=1,3\r"))).Do(c =>
                {
                    _serialPort.ReadExisting().Returns("\r\nERROR\r\n");
                    manager._receiveNow.Set();
                });

                // Act
                // Assert
                Assert.That(() => manager.DeleteMsg(_serialPort, "AT+CMGD=1,3"),
                    Throws.Exception.TypeOf<ApplicationException>()
                    .With.Message.EqualTo("No success message was received."));
            }
        }

        [SetUp]
        public void SetUpSerialPortMock()
        {
            _serialPort = Substitute.For<ISerialPort>();

            _serialPort.PortName = Arg.Do((string arg) =>
            {
                int num;
                if (!(arg != null && arg.ToUpper().StartsWith("COM") && int.TryParse(arg.Substring(3), out num) && num >= 0 && num <= 255))
                    throw new ArgumentException();
            });

            _serialPort.BaudRate = Arg.Do((int arg) =>
            {
                if (arg < 1 || arg > 115200)
                    throw new ArgumentOutOfRangeException();
            });

            _serialPort.DataBits = Arg.Do((int arg) =>
            {
                if (arg < 5 || arg > 8)
                    throw new ArgumentOutOfRangeException();
            });

            _serialPort.ReadTimeout = Arg.Do((int arg) =>
            {
                if (arg < 0 && arg != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException();
            });

            _serialPort.WriteTimeout = Arg.Do((int arg) =>
            {
                if (arg < 0 && arg != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException();
            });
        }

        [TearDown]
        public void TearDownSerialPortMock()
        {
            _serialPort = null;
        }
    }
}