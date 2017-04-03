package Place
{
  function getTrustLevel(%a,%b)
  {
    %groupA = getBrickGroupFromObject(%a);
    %groupB = getBrickGroupFromObject(%b);
    
    Parent::getTrustLevel(%a,%b);
  }
};
deactivatePackage("Place");
activatePackage("Place");
