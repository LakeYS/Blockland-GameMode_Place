$Pref::Server::PlaceTimer = 25;

deactivatePackage("Place");
package Place
{
  function GameConnection::sendTrustFailureMessage(%a, %b)
  {
    if(%b.bl_id == 50)
      return ""; // Blank.
    else
      Parent::sendTrustFailureMessage(%a, %b);
  }

  function PaintProjectile::onCollision(%datablock, %a, %b, %c, %d, %e)
  {
    Parent::onCollision(%datablock, %a, %b, %c, %d, %e);

    %groupA = getBrickGroupFromObject(%a); // Projectile
    %groupB = getBrickGroupFromObject(%B); // Brick
    %id = %groupA.client.bl_id;

    if($p_timeout[%id] || %groupB.potentialTrust[%id] != 2)
      return;

    // Set trust to 0 and set the timer.
    // Timing is stored globally instead of on client so it persists when they leave.
    setMutualBrickGroupTrust(50, %id, 0);
    $p_timeout[%id] = 1;
    $p_sprayTime[%id] = getRealTime()/1000;
    $p_sprayCount[%id]++;

    %groupA.client.canUndo = 1; // Allow them to use ctrl+z.

    // Set brick attributes
    %b.setName(" " @ %id); // Set the brick's name to the sprayer's ID.
  }

  function GameConnection::spawnPlayer(%player)
  {
    if(!$p_timeout[%player.bl_id])
      schedule(3000, 0, setMutualBrickGroupTrust, 50, %player.bl_id, 2);

    return Parent::spawnPlayer(%player);
  }

  //function GameModeInitialResetCheck()

  function destroyServer()
  {
    deleteVariables("$p_*"); // Delete variables on server shutdown.
  }

  function serverCmdUndoBrick(%client)
  {
    if(%client.canUndo)
    {
      setMutualBrickGroupTrust(50, %client.bl_id, 2);
      Parent::serverCmdUndoBrick(%client);
      setMutualBrickGroupTrust(50, %client.bl_id, 0);

      %client.canUndo = 0;
      $p_sprayTime[%client.bl_id] = 1; // Reset the timer
    }
  }

  function fxDTSBrick::onActivate(%brick, %player, %c, %d, %e)
  {
    %name = %brick.getName();
    if(%name $= "")
      %name = "None";


    %player.client.centerPrint("Placed by:<br>" @ %name,3);
    Parent::onActivate(%brick, %player, %c, %d, %e);
  }

  // # FX Can Projectiles
  function pearlPaintProjectile::onCollision() { }
  function chromePaintProjectile::onCollision() { }
  function glowPaintProjectile::onCollision() { }
  function blinkPaintProjectile::onCollision() { }
  function swirlPaintProjectile::onCollision() { }
  function rainbowPaintProjectile::onCollision() { }
  function stablePaintProjectile::onCollision() { }
  function jelloPaintProjectile::onCollision() { }

  // # Main Loop
  function place_loop()
  {
    for(%i = 0; %i <= clientGroup.getCount()-1; %i++)
    {
      %client = clientGroup.getObject(%i);
      %id = %client.bl_id;
      %time = getRealTime()/1000;

      if($p_timeout[%id])
      {
        if(%time >= $p_sprayTime[%id]+$Pref::Server::PlaceTimer)
        {
          %client.canUndo = 0;
          $p_timeout[%id] = 0;
          setMutualBrickGroupTrust(50, %id, 2);
        }
        else if(isObject(%client.player))
          %client.bottomPrint(getSubStr( getTimeString( $Pref::Server::PlaceTimer - (%time - $p_sprayTime[%id]) ),0,4 ), 2, 1);
      }
    }

    if(isEventPending($p_Loop))
    {
      error("GameMode_Place - Duplicate loop! Cancelling...");
      cancel($p_Loop);
    }

    $p_Loop = schedule(1000,0,place_loop);
  }
};
activatePackage("Place");

schedule(10000, 0, serverDirectSaveFileLoad, "Add-Ons/GameMode_Place/place_blank.bls", 3, "", 1); // Load the save.
place_loop(); // Start the loop.
