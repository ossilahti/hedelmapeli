using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author Ossi Lahti
/// @version 6.12.2019
/// <summary>
/// Hedelmäpeli, jossa tavoitteena kerätä mahdollisimman monta puusta putoavaa appelsiinia.
/// </summary>

// TODO: silmukka, ks: https://tim.jyu.fi/answers/kurssit/tie/ohj1/2019s/demot/demo5?answerNumber=8&task=kertotaulu&user=otvlahti
public class Hedelmapeli : PhysicsGame
{
    private readonly Image pallonKuva = LoadImage("apelsin");
    private readonly Image taustaKuva = LoadImage("harjoitustyonkuva");
    private readonly Image pomminKuva = LoadImage("pommi");
    private readonly Image korinKuva = LoadImage("kori");

    private Vector nopeusOikealle = new Vector(600, 0);
    private Vector nopeusVasemmalle = new Vector(-600, 0);

    private PhysicsObject vasenReuna;
    private PhysicsObject oikeaReuna;

    private PhysicsObject maila;
    private PhysicsObject lattia;

    private List<PhysicsObject> elamat;
    private IntMeter pisteLaskuri;


    public override void Begin() /// Alustetaan pelin aliohjelmakutsut.
    {
        LuoKentta();
        LuoPallo();
        LuoPommi();
        PelinNappaimet();
        PisteLaskuri();
        ElamienLisays();
        PelinLopetus();
    }


    /// <summary>
    /// Teen pelin kentälle oman aliohjelman, johon lisään pallojen silmukan, tietoa reunojen kimmoisuudesta ja 
    /// mailan aliohjelmakutsun.
    /// </summary>
    private void LuoKentta()
    {
        Timer timer = new Timer();
        timer.Interval = 1.30;
        timer.Timeout += LuoPallo;
        timer.Start();

        Timer timer2 = new Timer();
        timer2.Interval = 5.5000;
        timer2.Timeout += LuoPommi;
        timer2.Start();

        maila = LuoMaila(0.0, Level.Bottom + 30.0);
        lattia = LuoLattia(Level.Bottom - 100, Level.Bottom - 100);

        vasenReuna = Level.CreateLeftBorder();
        vasenReuna.Restitution = 1.0;
        vasenReuna.KineticFriction = 0.0;
        vasenReuna.IsVisible = false;

        oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.Restitution = 1.0;
        oikeaReuna.KineticFriction = 0.0;
        oikeaReuna.IsVisible = false;

        Level.Background.Image = taustaKuva;
        Level.Background.FitToLevel();

        Camera.ZoomToLevel();
    }


    /// <summary>
    /// Luodaan kentän alapuolelle lattia, joka kerää fysiikkaobjekteja itseensä.
    /// </summary>
    /// <param name="xx">tällä kuvataan x-koordinaattia</param>
    /// <param name="yy">tällä kuvataan y-koordinaattia</param>
    /// <returns>palauttaa lattian.</returns>
    private PhysicsObject LuoLattia(double xx, double yy)
    {
        PhysicsObject lattia = PhysicsObject.CreateStaticObject(Level.Width * 2, 5);
        lattia.Shape = Shape.Rectangle;
        lattia.X = xx;
        lattia.Y = yy;
        lattia.Restitution = 1.0;
        lattia.Tag = "lattia";
        Add(lattia);
        return lattia;
    }


    /// <summary>
    /// Aliohjelma, jossa luodaan hedelmä kentälle.
    /// </summary>
    private void LuoPallo()
    {
        PhysicsObject pallo = new PhysicsObject(50.0, 50.0, Shape.Circle);
        pallo.X = RandomGen.NextDouble(Level.Left, Level.Right);
        pallo.Y = RandomGen.NextDouble(Level.Top - 10, Level.Top - 50);
        pallo.Restitution = 1.0;
        pallo.Image = pallonKuva;
        Add(pallo);

        Vector impulssi = new Vector(0.0, -250.0);
        pallo.Hit(impulssi);

        AddCollisionHandler(pallo, "maila", Tormaysefekti);
        AddCollisionHandler(pallo, PisteLaskurinPistelisays);
        AddCollisionHandler(pallo, "lattia", PoistaHedelma);
        AddCollisionHandler(pallo, "lattia", PoistaElamiaHedelmat);

        pallo.CollisionIgnoreGroup = 1;
    }


    /// <summary>
    /// Aliohjelma, jossa luodaan pommi.
    /// </summary>
    private void LuoPommi()
    {
        PhysicsObject pommi = new PhysicsObject(50.0, 50.0, Shape.Circle);
        pommi.X = RandomGen.NextDouble(Level.Left, Level.Right);
        pommi.Y = RandomGen.NextDouble(Level.Top - 10, Level.Top - 50);
        pommi.Restitution = 1.0;
        pommi.Image = pomminKuva;
        Add(pommi);

        Vector pomminLiike = new Vector(0.0, -250.0);
        pommi.Hit(pomminLiike);

        pommi.CollisionIgnoreGroup = 1;

        AddCollisionHandler(pommi, "lattia", PoistaPommiLattia);
        AddCollisionHandler(pommi, "maila", PoistaPommiMaila);
        AddCollisionHandler(pommi, "maila", PoistaElamiaPommit);
    }


    /// <summary>
    /// Funktio, jossa luodaan maila eli kori peliin.
    /// </summary>
    /// <param name="x">x-koordinaatti</param>
    /// <param name="y">y-koordinaatti</param>
    /// <returns>palauttaa mailan</returns>
    private PhysicsObject LuoMaila(double x, double y)
    {
        PhysicsObject maila = PhysicsObject.CreateStaticObject(120.0, 70.0);
        maila.Tag = "maila";
        maila.Shape = Shape.Rectangle;
        maila.X = x;
        maila.Y = y;
        maila.Restitution = 1.0;
        maila.Image = korinKuva;
        Add(maila);
        return maila;
    }


    /// <summary>
    /// Aliohjelma, josta selviää pelin näppäimistö.
    /// </summary>
    private void PelinNappaimet()
    {
        Keyboard.Listen(Key.H, ButtonState.Down, ShowControlHelp, "Ohjekirja");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Pelin lopetus");

        Keyboard.Listen(Key.Right, ButtonState.Down, AsetaNopeus, "Liikuta mailaa oikealle", maila, nopeusOikealle);
        Keyboard.Listen(Key.Right, ButtonState.Released, AsetaNopeus, null, maila, Vector.Zero);
        Keyboard.Listen(Key.Left, ButtonState.Down, AsetaNopeus, "Liikuta mailaa vasemmalle", maila, nopeusVasemmalle);
        Keyboard.Listen(Key.Left, ButtonState.Released, AsetaNopeus, null, maila, Vector.Zero);
    }


    /// <summary>
    /// Jos maila osuu vasempaan tai oikeaan reunaan, sen nopeus muuttuu nollaksi ja pysähtyy. Täten maila ei poistu kuvaruudusta 
    /// missään välissä.
    /// </summary>
    /// <param name="maila">PhysicsObject maila, sille annetaan nopeus</param>
    /// <param name="nopeus">Mikä nopeus annetaan</param>
    private void AsetaNopeus(PhysicsObject maila, Vector nopeus)
    {
        if ((nopeus.X < 0) && (maila.Left < Level.Left))
        {
            maila.Velocity = Vector.Zero;
            return;
        }
        if ((nopeus.X > 0) && (maila.Right > Level.Right))
        {
            maila.Velocity = Vector.Zero;
            return;
        }
        maila.Velocity = nopeus;
    }


    /// <summary>
    /// Aliohjelma, jossa luodaan räjähdys "pallojen" osuessa "mailaan".
    /// </summary>
    /// <param name="pallo"></param>
    /// <param name="maila"></param>
    /// 
    private void Tormaysefekti(PhysicsObject pallo, PhysicsObject maila)
    {
        Explosion rajahdys = new Explosion(pallo.Width * 2);
        rajahdys.Position = pallo.Position;
        rajahdys.UseShockWave = false;
        Add(rajahdys);
        Remove(pallo);
    }


    /// <summary>
    /// Aliohjelma, josta pistelaskuria kutsutaan ja kerrotaan mikä pistelaskurin sijainti on.
    /// </summary>
    private void PisteLaskuri()
    {
        pisteLaskuri = LuoPisteLaskuri(Screen.Right - 100.0, Screen.Top - 100.0);
    }


    /// <summary>
    /// Funktio, jossa kerrotaan pistelaskurin tíedot ja kutsut.
    /// </summary>
    /// <param name="x">eli x-koordinaatti</param>
    /// <param name="y">eli y-koordinaatti</param>
    /// <returns>palauttaa laskurin PisteLaskuri-aliohjelmalle</returns>
    private IntMeter LuoPisteLaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0)
        {
            MaxValue = 9999
        };

        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.White;
        naytto.BorderColor = Color.Black;
        Add(naytto);

        return laskuri;
    }


    /// <summary>
    /// Aliohjelma, jossa kuvataan pelaajan elämiä. Kun hedelmä menee ohi korista, elämiä vähenee. 
    /// </summary>
    private PhysicsObject LuoElamat(PhysicsGame peli, double x, double y)
    {
        PhysicsObject sydan = new PhysicsObject(25.0, 25.0, Shape.Heart);
        sydan.Color = Color.Red;
        sydan.X = x;
        sydan.Y = y;
        sydan.CollisionIgnoreGroup = 1;
        Add(sydan);
        return sydan;
    }


    /// <summary>
    /// Aliohjelma, jossa elämät luodaan kentälle ja lisätään listaan.
    /// </summary>
    private void ElamienLisays()
    {
        elamat = new List<PhysicsObject>();

        PhysicsObject sydan1 = LuoElamat(this, Level.Left + 100, Level.Top - 100);
        elamat.Add(sydan1);
        PhysicsObject sydan2 = LuoElamat(this, Level.Left + 130, Level.Top - 100);
        elamat.Add(sydan2);
        PhysicsObject sydan3 = LuoElamat(this, Level.Left + 160, Level.Top - 100);
        elamat.Add(sydan3);
    }


    /// <summary>
    /// Funktio, jossa kerrotaan millä perusteella pisteitä tulee lisää. 
    /// </summary>
    /// <param name="pallo">tippuvat hedelmät eli pallo-objekti</param>
    /// <param name="kohde">pallon nimi on kohde</param>
    private void PisteLaskurinPistelisays(PhysicsObject pallo, PhysicsObject kohde)
    {
        if (kohde.Y == maila.Y)
        {
            pisteLaskuri.Value += 1;
        }
    }


    /// <summary>
    /// Kun pommi osuu lattiaan, se poistetaan pelistä. Tässä funktiossa on käsitelty AddCollisionHandler(pommi, "lattia", PoistaPommi).
    /// </summary>
    /// <param name="pommi">Pommi, joka osuu lattiaan</param>
    /// <param name="kohde">Pommi</param>
    private void PoistaPommiLattia(PhysicsObject pommi, PhysicsObject kohde)
    {
        if (kohde.Y == lattia.Y)
        {
            Remove(pommi);
        }
    }


    /// <summary>
    /// Funktio, jossa kerrotaan mitä tapahtuu kun pommi osuu mailaan. Eli silloin se häviää kentältä.
    /// </summary>
    /// <param name="pommi">Pommin nimi</param>
    /// <param name="kohde">Kohde joka osuu alustaan (pommi)</param>
    private void PoistaPommiMaila(PhysicsObject pommi, PhysicsObject kohde)
    {
        if (kohde.Y == maila.Y)
        {
            Remove(pommi);
        }
    }


    /// <summary>
    /// Funktio, jossa pallo osuu lattiaan ja poistuu pelistä. Tässä funktiossa on käsitelty AddCollisionHandler(pallo, "lattia", PoistaHedelma).
    /// </summary>
    /// <param name="pallo">Hedelmän nimi</param>
    /// <param name="kohde">Pallo</param>
    private void PoistaHedelma(PhysicsObject pallo, PhysicsObject kohde)
    {
        if (kohde.Y == lattia.Y)
        {
            Remove(pallo);
        }
    }


    /// <summary>
    /// Funktio, jossa käsitellän hedelmän osumista lattiaan, ja silloin kun se osuu lattiaan, hedelmä häviää, ja elämistä lähtee yksi pois.
    /// Funktiossa on käytetty switch-rakennetta käsittelemään elämien poistamista listasta ja kentältä.
    /// </summary>
    /// <param name="pallo">Hedelmän parametri</param>
    /// <param name="kohde">Kohde joka osuu lattiaan (hedelmä)</param>
    private void PoistaElamiaHedelmat(PhysicsObject pallo, PhysicsObject kohde)
    {
        if (kohde.Y == lattia.Y)
        {
            switch (elamat.Count)
            {
                case 3:
                    elamat[2].Destroy();
                    elamat.RemoveAt(2);
                    break;
                case 2:
                    elamat[1].Destroy();
                    elamat.RemoveAt(1);
                    break;
                case 1:
                    elamat[0].Destroy();
                    elamat.RemoveAt(0);
                    break;
            }
        }
        PelinLopetus();
    }

    /// <summary>
    /// Funktio, jossa käsitellän pommin osumista koriin, ja silloin kun se osuu koriin, pommi häviää, ja elämistä lähtee yksi pois.
    /// Funktiossa on käytetty switch-rakennetta käsittelemään elämien poistamista listasta ja kentältä.
    /// </summary>
    /// <param name="pommi">Pommin parametri</param>
    /// <param name="kohde">Kohde joka osuu mailaan (pommi)</param>
    private void PoistaElamiaPommit(PhysicsObject pommi, PhysicsObject kohde)
    {
        if (kohde.Y == maila.Y)
        {
            switch (elamat.Count)
            {
                case 3:
                    elamat[2].Destroy();
                    elamat.RemoveAt(2);
                    break;
                case 2:
                    elamat[1].Destroy();
                    elamat.RemoveAt(1);
                    break;
                case 1:
                    elamat[0].Destroy();
                    elamat.RemoveAt(0);
                    break;
            }
        }
        PelinLopetus();
    }

    /// <summary>
    /// Aliohjelma, jossa kerrotaan miten peli loppuu, ja mitä sitten tapahtuu.
    /// </summary>
    private void PelinLopetus()
    {
        if (elamat.Count == 0)
        {
            MultiSelectWindow valikko = new MultiSelectWindow("Peli päättyi.", "Aloita alusta", "Sulje peli");
            valikko.ItemSelected += MonivalintaIkkunanPainaminen;
            Add(valikko);
        }
    }


    /// <summary>
    /// Määritellään mitä tapahtuu, kun painaa monivalintaikkunoiden näppäimiä.
    /// </summary>
    /// <param name="valinta">valinta eli monivalintaikkunoiden näppäin.</param>
    private void MonivalintaIkkunanPainaminen(int valinta)
    {
        switch (valinta)
        {
            case 0: //Aloita alusta.
                ClearAll(); 
                Begin();
                break;
            case 1: // Sulje peli.
                Exit();
                break;
        }
    }
}



        
