$Pref::Server::PlaceTimer = 20;
$PlaceVer = 0.1;

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
    %groupA = getBrickGroupFromObject(%a); // Projectile
    %groupB = getBrickGroupFromObject(%B); // Brick
    %id = %groupA.client.bl_id;

    if($p_timeout[%id])
      return;

    // For security purposes, the actual trust is only sent when the color is applied.
    setMutualBrickGroupTrust(50, %id, 2);
    Parent::onCollision(%datablock, %a, %b, %c, %d, %e);
    setMutualBrickGroupTrust(50, %id, 0);

    // Timing is stored globally instead of on client so it persists when they leave.
    $p_timeout[%id] = 1;
    $p_sprayTime[%id] = getRealTime()/1000;
    $p_sprayCount[%id]++;

    $PlaceActive = 1;

    %groupA.client.canUndo = 1; // Allow them to use ctrl+z.

    // Set brick attributes
    %b.setNTObjectName(%id); // Set the brick's name to the sprayer's ID.
  }

  //function GameModeInitialResetCheck()

  function destroyServer()
  {
    Parent::destroyServer();

    echo("Exporting place stats");
    export("$p_*","config/server/placeStats.cs");

    $PlaceLoaded = 0;
    deleteVariables("$p_*"); // Delete variables on server shutdown.
  }

  function serverCmdUndoBrick(%client)
  {
    // Note: Doesn't roll brick name back.
    if(%client.canUndo)
    {
      setMutualBrickGroupTrust(50, %client.bl_id, 2);
      Parent::serverCmdUndoBrick(%client);
      setMutualBrickGroupTrust(50, %client.bl_id, 0);

      $PlaceActive = 1;

      %client.canUndo = 0;
      $p_sprayTime[%client.bl_id] = 1; // Reset the timer
    }
  }

  function fxDTSBrick::onActivate(%brick, %player, %c, %d, %e)
  {
    %name = %brick.getName();
    %name = getSubStr(%name,1,strlen(%name)+1);

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

  // # Save Loop
  function place_export_loop()
  {
    if($PlaceActive) // Only export if active.
    {
      echo("Exporting place stats");
      export("$p_*","config/server/placeStats.cs");
      $PlaceActive = 0;
    }

    if(isEventPending($p_loopSave))
    {
      error("GameMode_Place - Duplicate loop! Cancelling...");
      cancel($p_loopSave);
    }

    $p_loopSave = schedule(600000,0,place_export_loop); // Every 10min
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

if(!$PlaceLoaded)
{
  $PlaceLoaded = 1;

  // Load optional add-on: autosaver
  if(isFile("Add-Ons/Support_AutoSaver/server.cs"))
    exec("Add-Ons/Support_AutoSaver/server.cs");

  if(isFile("config/server/placeStats.cs"))
    exec("config/server/placeStats.cs");

  schedule(5000, 0, serverDirectSaveFileLoad, "Add-Ons/GameMode_Place/place_blank.bls", 3, "", 1); // Load the save.

  place_loop(); // Start the loop.
  $p_loopSave = schedule(600000,0,place_export_loop); // Start the export loop.
}
