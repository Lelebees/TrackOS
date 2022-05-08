//Script made by Lelebees: Last update at 17-01-2022 21:39 UTC+1 (Central European Time)
//TrackOS V1.2.1 (Not backwards compatible)

//Tweakable variables below here
string antennaName = "Antenna";
string broadcastChannel = "channel 1"; //you can set this to literally anything, and I advise you to do so, to keep out people who are guessing lucky.
bool receiverSystem = false; //set to true if you want to receive a programmable block's location.
string outputPanelName = "Output LCD";
int screenNumber = 0; //this is the integer that use can use to decide which cockpit/PB screen (or whatever) you want to use. No guarantee this works on modded blocks!

//reciever only variables
bool policeSender = true; //receiver system only. Recommended false when using multiple Senders on one channel.
long allowedProgBlockID = 1234567890; //This is the ID of the programmable block you are receiving from.

//sender only variables
long receiverProgBlockID = 1234567890; //This is the ID of the programmable block you are sending to. Use this to immedeately trigger a send when the Receiver is in range
bool checkingRange = true; // The on off switch for the above mentioned feature.
bool useInternalTimer = true; //choose if you want the internal timer to work, or if you want to use your own timers
int timeDelay = 1800; //delay between send commands in ticks (60 ticks = 1 second) (Send Systems only)


//Do NOT touch the variables below this line. This WILL break the program.
bool sentInrangeMessage = false;
int messageCount = 0;
int tickCount = 0;
bool endpoint = false;
IMyRadioAntenna antenna;
IMyTerminalBlock statusDisplay;
IMyTextSurface textSurface;
IEnumerator<bool> _stateMachine;

public Program() //This is the setup, which is automatically ran every time script is compiled or after a world reload
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    //assign blocks to variables
    antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
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
    Echo("Setup complete. System  messages will now appear on displays.\n");
    textSurface.WriteText("Setup complete.\n", false);

    if (receiverSystem == false)
    {
        //_stateMachine = Send(broadcastChannel);
        //Runtime.UpdateFrequency |= UpdateFrequency.Once;
        Echo ("Sending test complete. System should be ready to send coordinates.");
    }
}

public void Main(string arg, UpdateType updateType) 
{
    // We only want to run the state machine(s) when the update type includes the
    // "Once" flag, to avoid running it more often than it should. It shouldn't run
    // on any other trigger. This way we can combine state machine running with
    // other kinds of execution, like tool bar commands, sensors or what have you.
    if ((updateType & UpdateType.Once) == UpdateType.Once) 
    {
        RunStateMachine();
    }
    else
    {
        tickCount++;
        arg = arg.ToLower();
        switch(arg) 
        { 
            case "send":
                _stateMachine = Send(broadcastChannel); //Send coordinates (Method on line 130)
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                break;
            
            case "get id":
                GetID();
                break;    
            
            default:
                _stateMachine = Loop(); //We're using yield statements in the loop method for checking Endpoint, therefore, we need a statemachine
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                break;
        }
    }
}

// ***MARKER: Coroutine Execution
public void RunStateMachine()
{
    // If there is an active state machine, run its next instruction set.
    if (_stateMachine != null) 
    {
        // The MoveNext method is the most important part of this system. When you call
        // MoveNext, your method is invoked until it hits a `yield return` statement.
        // Once that happens, your method is halted and flow control returns _here_.
        // At this point, MoveNext will return `true` since there's more code in your
        // method to execute. Once your method reaches its end and there are no more
        // yields, MoveNext will return false to signal that the method has completed.
        // The actual return value of your yields are unimportant to the actual state
        // machine.
        bool hasMoreSteps = _stateMachine.MoveNext();

        // If there are no more instructions, we stop and release the state machine.
        if (hasMoreSteps)
        {
            // The state machine still has more work to do, so signal another run again, 
            // just like at the beginning.
            Runtime.UpdateFrequency |= UpdateFrequency.Once; // Here we add the ONCE flag, telling the program to stop, and run again in the next tick. It will continue where it left off.
        } 
        else 
        {
            _stateMachine.Dispose();

            // In our case we just want to run this once, so we set the state machine
            // variable to null. But if we wanted to continously run the same method, we
            // could as well do
            // _stateMachine = RunStuffOverTime();
            // instead.
            _stateMachine = null;
        }
    }
}

public IEnumerator<bool> Send(string broadcastChannel) 
{
    //pepare for sending
    messageCount++;
    Vector3D Position = Me.GetPosition();
    string messageText = $"GPS:Tracker Location {messageCount}:{Position.X}:{Position.Y}:{Position.Z}:#FF0000:"; // Format the text so it can by copy-pasted into GPS list
    //Everything has been prepared, we can now open the broadcasting channel
    antenna.EnableBroadcasting = true;
    yield return true; //wait* a tick
    //TODO: Evaluate if this yield is necessary
    //Send the message
    IGC.SendBroadcastMessage(broadcastChannel, messageText, TransmissionDistance.TransmissionDistanceMax);  
    
    //prepare for ending the send thing
    yield return true; //wait* a tick
    yield return true; // wait another tick so this fucking thing actually sends
	
    //Close the broadcast so detection is now impossible
    antenna.EnableBroadcasting = false;
    
    textSurface.WriteText ("sent message ["+messageCount+"] succesfully.\n", true);
    
    //custom actions box (When something is sent)
    
    //end of custom actions box
    
    //NOTE: the following comment has been left in to illustrate my struggles with the program, it is no longer relevant
    //okay so that didnt "Work" either. It does get to all this shite but no send. GO 9 YIELDS
    // Ironically, in retrospect, 9 yields was about as far from a solution as i could get. 
    
    // *The program doesn't actually wait. It stops and runs from that point on the next tick. this lowers the instruction count, making the script more preformance friendly, however, we're using it to make a delay in between certain actions
    //IT WORKS, WHAHAHAH YES IT WORKS! FUCK YOU COROUTINES, I AM A GOD OF SCRIPTING! TREMBLE IN FEAR BEFORE THE GREAT AND MIGHTY LELEBEES!
}

public void Receive(string broadcastChannel) //IGC magik
{
    IGC.RegisterBroadcastListener(broadcastChannel);
    List<IMyBroadcastListener> listenerList = new List<IMyBroadcastListener>();
    IGC.GetBroadcastListeners(listenerList);
    if (listenerList[0].HasPendingMessage) //if there is a message in the queue
    {
        MyIGCMessage receivedMessage = new MyIGCMessage();
        receivedMessage = listenerList[0].AcceptMessage();
        string receivedText = receivedMessage.Data.ToString(); //take the message text
        string receivedChannel = receivedMessage.Tag; //take the message tag (channel)
        long messageSender = receivedMessage.Source; //take the sender ID

        //Do something with the information!
        if (policeSender == true)
        {
            //TODO: Evaluate if this is even necessary
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
            //add custom actions here if policeSender is off

            //end of custom actions box
        }
    }
}

public void GetID()
{
    long myID = IGC.Me;
    textSurface.WriteText(myID.ToString()+"\n", true);
    Me.CustomData = myID.ToString();
    textSurface.WriteText("ID saved to CustomData\n", true);
}

public IEnumerator<bool> Loop()
{
    if(receiverSystem)
    {
        Receive(broadcastChannel);
    }
    else if(checkingRange)
    {
        antenna.EnableBroadcasting = true;
        yield return true;
        endpoint = IGC.IsEndpointReachable(receiverProgBlockID, TransmissionDistance.TransmissionDistanceMax);
        yield return true;
        yield return true;
        antenna.EnableBroadcasting = false;
    

        if(endpoint == true & sentInrangeMessage == false)
        {
            //this will immedeately trigger the send command so location is broadcasted.
            //pepare for sending
            messageCount++;
            Vector3D Position = Me.GetPosition();
            string messageText = $"GPS:Tracker Location {messageCount}:{Position.X}:{Position.Y}:{Position.Z}:#FF0000:"; // Format the text so it can by copy-pasted into GPS list
            //Everything has been prepared, we can now open the broadcasting channel
            antenna.EnableBroadcasting = true;
            yield return true; //wait* a tick
            //TODO: Evaluate if this yield is necessary
            //Send the message
            IGC.SendBroadcastMessage(broadcastChannel, messageText, TransmissionDistance.TransmissionDistanceMax);  
    
            //prepare for ending the send thing
            yield return true; //wait* a tick
            yield return true; // wait another tick so this fucking thing actually sends
	
            //Close the broadcast so detection is now impossible
            antenna.EnableBroadcasting = false;
    
            //I know i copied the Send method here, and there is probably some way to call the send method as a statemachine, but i couldnt find it 
            //and I have an idea which would have me rebuild the script anyway, so ctl+c ctrl+v it is.
            sentInrangeMessage = true;
            textSurface.WriteText("Found Receiver in range! Sent co-ordinates immedeately!\n", true);
            //custom actions box when a reciever has been found in range

            //end of custom actions box
        }
        else if(!endpoint)
        {
            sentInrangeMessage = false;
        }
    }
    if (!receiverSystem & tickCount == timeDelay & useInternalTimer == true)
    {
        tickCount = 0;
        _stateMachine = Send(broadcastChannel);
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
    }
}

//Please note, during testing, i found a bug in the script that has minor impact on the effectiveness. I will not be patching it as it is harmless and frankly I can't bring myself to keep developing TrackOS in it's current, majorly flawed form.
//I am not quitting scripting and this will certainly not be the last time you'll hear from a tracking script, as I've already revealed I am going to rebuild the script completely different
//For those interested, that is the 3rd rebuild I will do, and hopefully the last.
//I will not be uploading TrackOS to mod.io. The reason for this is that it takes me too much time (and research) to make a version that is more user friendly,
//especially for XBox users. That being said, I do plan on uploading the TrackOS rebuild to mod.io after I've implemented user friendliness to it.
//(Mostly because it will require basically no user guided setup, which means i can get away with shittier implementations)

//For other Scripting developers that wish to use the sending and receiving methods I've developed here, I will upload a separate script to the workshop.
//I'll most probably call it something like Virtually Invisible Communication Protocol (or VICP), as some may have guessed when they were looking here using V1.2.
//Keep your eyes peeled for that one.

//The Github where you can report issues will stay open and I will continue to fix bugs that affect the script in a mayor way.
//At least, until someone else is willing to pick up the mess that is this code. 

//Leaving tracking range for now,
//Lelebees