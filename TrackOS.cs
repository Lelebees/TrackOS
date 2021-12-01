//Script made by Lelebees: Last update at 10-01-2021 18:44 UTC+1 (Central European Time)

//Tweakable variables below here

string antennaName = "Antenna";
string broadcastChannel = "channel 1"; //you can set this to literally anything, and I advise you to do so, to keep out people who are guessing lucky.
bool receiverSystem = false; //set to true if you want to receive a programmable block's location.
string outputPanelName = "Output LCD";
int screenNumber = 0;

bool policeSender = false; //receiver system only. Recommended false when using multiple Senders on one channel.
long allowedProgBlockID = 1234567890; //This is the ID of the programmable block you are receiving from.

string attachedTimerName = "Timer Block";
long receiverProgBlockID = 1234567890; //This is the ID of the programmable block you are sending to. Use this to immedeately trigger a send when the Receiver is in range
bool checkingRange = true;

//Do NOT touch the variables below this line unless you know what you are doing! This WILL break the program.

bool setup = false;
IMyRadioAntenna antenna;
int messageCount = 0;
bool sentInrangeMessage = false;
IMyTerminalBlock statusDisplay;
IMyTextSurface textSurface;
IMyTimerBlock timer;

public void Main(string arg)
{
    //Make the script autorun
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    //Check if we've ran the setup
    if (!setup)
    {
        Echo("Setup incomplete, running setup...\n");
        Setup();
    } //this if statement could probably be reversed but I'm too lazy
    else
    {
        switch(arg) 
        { // Here, we're taking a look at the input. Since it's case sensitive, we use the different "case" calls right under eachother, creating an or effect.
            case "send":
            case "Send":
            case "SEND":
                //If we are supposed to send (will clean this up later)
                antenna.EnableBroadcasting = true;
                Send(Me.GetPosition().ToString(), broadcastChannel);
                timer.StartCountdown();
                break;
            
            case "get id":
            case "Get id":
            case "GET id":
            case "get ID":
            case "Get ID":
            case "GET ID":
                //If player requests PB ID
                GetID();
                break;    
            
            default:
                //If there is no argument given (keeps the program running)
                Loop();
                break;
        }
    }
}

public void Setup()
{
    //Assign blocks to variables
    antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
    timer = GridTerminalSystem.GetBlockWithName(attachedTimerName) as IMyTimerBlock;
    statusDisplay = GridTerminalSystem.GetBlockWithName(outputPanelName);
    //Decide what type of TextSurface the display is.
    if (statusDisplay is IMyTextSurface)
    {
        textSurface = (IMyTextSurface)statusDisplay;
    }
    else if (statusDisplay is IMyTextSurfaceProvider)
    {
        textSurface = ((IMyTextSurfaceProvider)statusDisplay).GetSurface(screenNumber);
    }
    else
    {
        textSurface = ((IMyTextSurfaceProvider)Me).GetSurface(screenNumber);
    }
    //Tell the player the setup was successfull by using both Echo and the textSurface
    Echo("Setup complete. Further output will be displayed on the connected LCD\n");
    textSurface.WriteText("Setup complete.\n", false);
    //Make sure we dont need to run the setup again
    setup = true;
}

public void Send(string messageText, string broadcastChannel)
{
    IGC.SendBroadcastMessage(broadcastChannel, messageText, TransmissionDistance.TransmissionDistanceMax);
	messageCount++;
	textSurface.WriteText ("sent message ["+messageCount+"] succesfully.\n", true);
	antenna.EnableBroadcasting = false;
    //add custom actions the program must perform after sending a message here!

    //end of custom actions box
}

public void Receive(string broadcastChannel)
{
    //We use programming magic to create a listener, that will help us receive messages
    IGC.RegisterBroadcastListener(broadcastChannel);
    List<IMyBroadcastListener> listenerList = new List<IMyBroadcastListener>();
    IGC.GetBroadcastListeners(listenerList);
    if (listenerList[0].HasPendingMessage) //if there is a message that we haven't read yet:
    {
        MyIGCMessage receivedMessage = new MyIGCMessage();
        receivedMessage = listenerList[0].AcceptMessage();
        //"accept" the message and convert it to a readable format.
        string receivedText = receivedMessage.Data.ToString();
        string receivedChannel = receivedMessage.Tag;
        long messageSender = receivedMessage.Source;

        //Do something with the information!
        if (policeSender == true)
        {
            if (messageSender == allowedProgBlockID)
            {
                textSurface.WriteText("Message received on channel "+receivedChannel+"\n", true);
                textSurface.WriteText("Message content: "+receivedText+"\n", true);
                //add custom actions here if policeSender is on and it is the correct sender

                //end of custom actions box
            }
            else
            {
                textSurface.WriteText("Recieved message from unlisted reciever "+messageSender.ToString()+" on channel "+receivedChannel+"\n", true);
                //custom actions when incorrect sender received

                //end of custom actions box
            }
        }
        else
        {
            textSurface.WriteText("Message received on channel "+receivedChannel+"\n", true);
            textSurface.WriteText("Message content: "+receivedText+"\n", true);
            //add custom actions here if policeSender is off and a message is received

            //end of custom actions box
        }
    }
}

public void GetID()
{
    //gets the current programmable block's ID and pastes it into the Custom Data
    long myID = IGC.Me;
    textSurface.WriteText(myID.ToString()+"\n", true);
    Me.CustomData = myID.ToString();
    textSurface.WriteText("ID saved to CustomData\n", true);
}

public void Loop()
{
    
    if(receiverSystem)
    {
        Receive(broadcastChannel);
    }
    else if(checkingRange) //If we're not receiving and we are checking if a receiver is in range:
    {
        bool endpoint = IGC.IsEndpointReachable(receiverProgBlockID, TransmissionDistance.TransmissionDistanceMax);
        
        if(endpoint & sentInrangeMessage == false)
        {
            //this will immedeately trigger the timer (which you should have set up to turn on the Antenna and run the PB with the send command) so location is broadcasted.
            timer.Trigger(); //This one will activate the Send option.
            sentInrangeMessage = true;
            textSurface.WriteText("Found Receiver in range! Sent co-ordinates immedeately!\n", false);
            //custom actions box when a reciever has been found in range

            //end of custom actions box
        }
        else if(!endpoint)
        {
            sentInrangeMessage = false;
        }
    }
}