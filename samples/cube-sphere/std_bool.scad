difference ()
{
    color([1,0,0])
    cube ([10,10,10]);

    color([0,0,1])
    translate ([10,0,10])
    sphere (r=5);
}

translate ([20,0,0])
union()
{
    color([1,0,0])
    cube ([10,10,10]);

    color([0,0,1])
    translate ([10,0,10])
    sphere (r=5);
}

translate ([40,0,0])
intersection()
{
    color([1,0,0])
    cube ([10,10,10]);

    color([0,0,1])
    translate ([10,0,10])
    sphere (r=5);
}
