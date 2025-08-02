#!/bin/bash

# Create fake music library structure for Navidrome
# Using white-noise.m4a as the base file for all tracks

BASE_FILE="white-noise.m4a"

# Check if base file exists
if [ ! -f "$BASE_FILE" ]; then
    echo "Error: $BASE_FILE not found!"
    exit 1
fi

echo "Creating music library structure..."

# Album 1: Pink Floyd - The Dark Side of the Moon (1973)
mkdir -p "Pink Floyd/The Dark Side of the Moon (1973)"
cd "Pink Floyd/The Dark Side of the Moon (1973)"
cp "../../$BASE_FILE" "01 - Speak to Me.m4a"
cp "../../$BASE_FILE" "02 - Breathe (In the Air).m4a"
cp "../../$BASE_FILE" "03 - On the Run.m4a"
cp "../../$BASE_FILE" "04 - Time.m4a"
cp "../../$BASE_FILE" "05 - The Great Gig in the Sky.m4a"
cp "../../$BASE_FILE" "06 - Money.m4a"
cp "../../$BASE_FILE" "07 - Us and Them.m4a"
cp "../../$BASE_FILE" "08 - Any Colour You Like.m4a"
cp "../../$BASE_FILE" "09 - Brain Damage.m4a"
cp "../../$BASE_FILE" "10 - Eclipse.m4a"
cd ../..

# Album 2: The Beatles - Abbey Road (1969)
mkdir -p "The Beatles/Abbey Road (1969)"
cd "The Beatles/Abbey Road (1969)"
cp "../../$BASE_FILE" "01 - Come Together.m4a"
cp "../../$BASE_FILE" "02 - Something.m4a"
cp "../../$BASE_FILE" "03 - Maxwell's Silver Hammer.m4a"
cp "../../$BASE_FILE" "04 - Oh! Darling.m4a"
cp "../../$BASE_FILE" "05 - Octopus's Garden.m4a"
cp "../../$BASE_FILE" "06 - I Want You (She's So Heavy).m4a"
cp "../../$BASE_FILE" "07 - Here Comes the Sun.m4a"
cp "../../$BASE_FILE" "08 - Because.m4a"
cp "../../$BASE_FILE" "09 - You Never Give Me Your Money.m4a"
cp "../../$BASE_FILE" "10 - Sun King.m4a"
cp "../../$BASE_FILE" "11 - Mean Mr. Mustard.m4a"
cp "../../$BASE_FILE" "12 - Polythene Pam.m4a"
cp "../../$BASE_FILE" "13 - She Came in Through the Bathroom Window.m4a"
cp "../../$BASE_FILE" "14 - Golden Slumbers.m4a"
cp "../../$BASE_FILE" "15 - Carry That Weight.m4a"
cp "../../$BASE_FILE" "16 - The End.m4a"
cp "../../$BASE_FILE" "17 - Her Majesty.m4a"
cd ../..

# Album 3: Michael Jackson - Thriller (1982)
mkdir -p "Michael Jackson/Thriller (1982)"
cd "Michael Jackson/Thriller (1982)"
cp "../../$BASE_FILE" "01 - Wanna Be Startin' Somethin'.m4a"
cp "../../$BASE_FILE" "02 - Baby Be Mine.m4a"
cp "../../$BASE_FILE" "03 - The Girl Is Mine.m4a"
cp "../../$BASE_FILE" "04 - Thriller.m4a"
cp "../../$BASE_FILE" "05 - Beat It.m4a"
cp "../../$BASE_FILE" "06 - Billie Jean.m4a"
cp "../../$BASE_FILE" "07 - Human Nature.m4a"
cp "../../$BASE_FILE" "08 - P.Y.T. (Pretty Young Thing).m4a"
cp "../../$BASE_FILE" "09 - The Lady in My Life.m4a"
cd ../..

# Album 4: Led Zeppelin - Led Zeppelin IV (1971)
mkdir -p "Led Zeppelin/Led Zeppelin IV (1971)"
cd "Led Zeppelin/Led Zeppelin IV (1971)"
cp "../../$BASE_FILE" "01 - Black Dog.m4a"
cp "../../$BASE_FILE" "02 - Rock and Roll.m4a"
cp "../../$BASE_FILE" "03 - The Battle of Evermore.m4a"
cp "../../$BASE_FILE" "04 - Stairway to Heaven.m4a"
cp "../../$BASE_FILE" "05 - Misty Mountain Hop.m4a"
cp "../../$BASE_FILE" "06 - Four Sticks.m4a"
cp "../../$BASE_FILE" "07 - Going to California.m4a"
cp "../../$BASE_FILE" "08 - When the Levee Breaks.m4a"
cd ../..

# Album 5: Nirvana - Nevermind (1991)
mkdir -p "Nirvana/Nevermind (1991)"
cd "Nirvana/Nevermind (1991)"
cp "../../$BASE_FILE" "01 - Smells Like Teen Spirit.m4a"
cp "../../$BASE_FILE" "02 - In Bloom.m4a"
cp "../../$BASE_FILE" "03 - Come as You Are.m4a"
cp "../../$BASE_FILE" "04 - Breed.m4a"
cp "../../$BASE_FILE" "05 - Lithium.m4a"
cp "../../$BASE_FILE" "06 - Polly.m4a"
cp "../../$BASE_FILE" "07 - Territorial Pissings.m4a"
cp "../../$BASE_FILE" "08 - Drain You.m4a"
cp "../../$BASE_FILE" "09 - Lounge Act.m4a"
cp "../../$BASE_FILE" "10 - Stay Away.m4a"
cp "../../$BASE_FILE" "11 - On a Plain.m4a"
cp "../../$BASE_FILE" "12 - Something in the Way.m4a"
cd ../..

# Album 6: AC/DC - Back in Black (1980)
mkdir -p "AC DC/Back in Black (1980)"
cd "AC DC/Back in Black (1980)"
cp "../../$BASE_FILE" "01 - Hells Bells.m4a"
cp "../../$BASE_FILE" "02 - Shoot to Thrill.m4a"
cp "../../$BASE_FILE" "03 - What Do You Do for Money Honey.m4a"
cp "../../$BASE_FILE" "04 - Given the Dog a Bone.m4a"
cp "../../$BASE_FILE" "05 - Let Me Put My Love into You.m4a"
cp "../../$BASE_FILE" "06 - Back in Black.m4a"
cp "../../$BASE_FILE" "07 - You Shook Me All Night Long.m4a"
cp "../../$BASE_FILE" "08 - Have a Drink on Me.m4a"
cp "../../$BASE_FILE" "09 - Shake a Leg.m4a"
cp "../../$BASE_FILE" "10 - Rock and Roll Ain't Noise Pollution.m4a"
cd ../..

# Album 7: Eagles - Hotel California (1976)
mkdir -p "Eagles/Hotel California (1976)"
cd "Eagles/Hotel California (1976)"
cp "../../$BASE_FILE" "01 - Hotel California.m4a"
cp "../../$BASE_FILE" "02 - New Kid in Town.m4a"
cp "../../$BASE_FILE" "03 - Life in the Fast Lane.m4a"
cp "../../$BASE_FILE" "04 - Wasted Time.m4a"
cp "../../$BASE_FILE" "05 - Wasted Time (Reprise).m4a"
cp "../../$BASE_FILE" "06 - Victim of Love.m4a"
cp "../../$BASE_FILE" "07 - Pretty Maids All in a Row.m4a"
cp "../../$BASE_FILE" "08 - Try and Love Again.m4a"
cp "../../$BASE_FILE" "09 - The Last Resort.m4a"
cd ../..

# Album 8: Dr. Dre - The Chronic (1992)
mkdir -p "Dr. Dre/The Chronic (1992)"
cd "Dr. Dre/The Chronic (1992)"
cp "../../$BASE_FILE" "01 - The Chronic (Intro).m4a"
cp "../../$BASE_FILE" "02 - Fuck wit Dre Day (And Everybody's Celebratin').m4a"
cp "../../$BASE_FILE" "03 - Let Me Ride.m4a"
cp "../../$BASE_FILE" "04 - The Day the Niggaz Took Over.m4a"
cp "../../$BASE_FILE" "05 - Nuthin' but a 'G' Thang.m4a"
cp "../../$BASE_FILE" "06 - Deeez Nuuuts.m4a"
cp "../../$BASE_FILE" "07 - Lil' Ghetto Boy.m4a"
cp "../../$BASE_FILE" "08 - A Nigga Witta Gun.m4a"
cp "../../$BASE_FILE" "09 - Rat-Tat-Tat-Tat.m4a"
cp "../../$BASE_FILE" "10 - The $20 Sack Pyramid.m4a"
cp "../../$BASE_FILE" "11 - Lyrical Gangbang.m4a"
cp "../../$BASE_FILE" "12 - High Powered.m4a"
cp "../../$BASE_FILE" "13 - The Doctor's Office.m4a"
cp "../../$BASE_FILE" "14 - Stranded on Death Row.m4a"
cp "../../$BASE_FILE" "15 - The Roach (The Chronic Outro).m4a"
cp "../../$BASE_FILE" "16 - Bitches Ain't Shit.m4a"
cd ../..

# Album 9: Nas - Illmatic (1994)
mkdir -p "Nas/Illmatic (1994)"
cd "Nas/Illmatic (1994)"
cp "../../$BASE_FILE" "01 - The Genesis.m4a"
cp "../../$BASE_FILE" "02 - N.Y. State of Mind.m4a"
cp "../../$BASE_FILE" "03 - Life's a Bitch.m4a"
cp "../../$BASE_FILE" "04 - The World Is Yours.m4a"
cp "../../$BASE_FILE" "05 - Halftime.m4a"
cp "../../$BASE_FILE" "06 - Memory Lane (Sittin' in da Park).m4a"
cp "../../$BASE_FILE" "07 - One Love.m4a"
cp "../../$BASE_FILE" "08 - One Time 4 Your Mind.m4a"
cp "../../$BASE_FILE" "09 - Represent.m4a"
cp "../../$BASE_FILE" "10 - It Ain't Hard to Tell.m4a"
cd ../..

# Album 10: The Notorious B.I.G. - Ready to Die (1994)
mkdir -p "The Notorious B.I.G/Ready to Die (1994)"
cd "The Notorious B.I.G/Ready to Die (1994)"
cp "../../$BASE_FILE" "01 - Intro.m4a"
cp "../../$BASE_FILE" "02 - Things Done Changed.m4a"
cp "../../$BASE_FILE" "03 - Gimme the Loot.m4a"
cp "../../$BASE_FILE" "04 - Machine Gun Funk.m4a"
cp "../../$BASE_FILE" "05 - Warning.m4a"
cp "../../$BASE_FILE" "06 - Ready to Die.m4a"
cp "../../$BASE_FILE" "07 - One More Chance.m4a"
cp "../../$BASE_FILE" "08 - Fuck Me (Interlude).m4a"
cp "../../$BASE_FILE" "09 - The What.m4a"
cp "../../$BASE_FILE" "10 - Juicy.m4a"
cp "../../$BASE_FILE" "11 - Everyday Struggle.m4a"
cp "../../$BASE_FILE" "12 - Me & My Bitch.m4a"
cp "../../$BASE_FILE" "13 - Big Poppa.m4a"
cp "../../$BASE_FILE" "14 - Respect.m4a"
cp "../../$BASE_FILE" "15 - Friend of Mine.m4a"
cp "../../$BASE_FILE" "16 - Unbelievable.m4a"
cp "../../$BASE_FILE" "17 - Suicidal Thoughts.m4a"
cd ../..

echo "Music library created successfully!"
echo "Total albums: 10"
echo "Artists: Pink Floyd, The Beatles, Michael Jackson, Led Zeppelin, Nirvana, AC/DC, Eagles, Dr. Dre, Nas, The Notorious B.I.G."
echo ""
echo "Directory structure:"
find . -type d -name "*(*)" | sort
