$Pref::Server::PlaceTimer = 60;

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

    // Set brick attributes
    %b.setName(" " @ %id); // Set the brick's name to the sprayer's ID.
  }

  function GameConnection::spawnPlayer(%player)
  {
    echo("client entered game, applying trust");
    if(!$p_timeout[%player.bl_id])
      schedule(3000, 0, setMutualBrickGroupTrust, 50, %player.bl_id, 2);

    return Parent::spawnPlayer(%player);
  }

  // This function is called when save.bls is finished loading
  function GameModeInitialResetCheck()
  {
    Parent::GameModeInitialResetCheck();

    place_loop(); // Start the loop.
    schedule(3000, 0, serverDirectSaveFileLoad, "Add-Ons/GameMode_Place/place_blank.bls", 3, "", 1); // Load the save.
  }

  function destroyServer()
  {
    deleteVariables("$p_*"); // Delete variables on server shutdown.
  }

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
          $p_timeout[%id] = 0;
          setMutualBrickGroupTrust(50, %id, 2);
        }
        else
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
