#!/bin/bash

# Create fake music library structure for Navidrome with proper metadata
# Using white-noise.m4a as the base file for all tracks

BASE_FILE="white-noise.m4a"

# Check if base file exists
if [ ! -f "$BASE_FILE" ]; then
    echo "Error: $BASE_FILE not found!"
    exit 1
fi

# Check if ffmpeg is available
if ! command -v ffmpeg &> /dev/null; then
    echo "Error: ffmpeg is required but not installed!"
    echo "Please install ffmpeg: brew install ffmpeg"
    exit 1
fi

echo "Creating music library structure with metadata..."

# Function to create track with metadata
create_track() {
    local track_num="$1"
    local title="$2"
    local artist="$3"
    local album="$4"
    local year="$5"
    local genre="$6"
    local filename="$7"

    ffmpeg -i "../../$BASE_FILE" \
        -metadata track="$track_num" \
        -metadata title="$title" \
        -metadata artist="$artist" \
        -metadata album="$album" \
        -metadata date="$year" \
        -metadata genre="$genre" \
        -codec copy \
        -y "$filename" 2>/dev/null

    if [ $? -eq 0 ]; then
        echo "  ✓ Created: $filename"
    else
        echo "  ✗ Failed: $filename"
    fi
}

# Album 1: Pink Floyd - The Dark Side of the Moon (1973)
echo "Creating Pink Floyd - The Dark Side of the Moon (1973)..."
mkdir -p "Pink Floyd/The Dark Side of the Moon (1973)"
cd "Pink Floyd/The Dark Side of the Moon (1973)"

create_track "1" "Speak to Me" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "01 - Speak to Me.m4a"
create_track "2" "Breathe (In the Air)" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "02 - Breathe (In the Air).m4a"
create_track "3" "On the Run" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "03 - On the Run.m4a"
create_track "4" "Time" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "04 - Time.m4a"
create_track "5" "The Great Gig in the Sky" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "05 - The Great Gig in the Sky.m4a"
create_track "6" "Money" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "06 - Money.m4a"
create_track "7" "Us and Them" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "07 - Us and Them.m4a"
create_track "8" "Any Colour You Like" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "08 - Any Colour You Like.m4a"
create_track "9" "Brain Damage" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "09 - Brain Damage.m4a"
create_track "10" "Eclipse" "Pink Floyd" "The Dark Side of the Moon" "1973" "Progressive Rock" "10 - Eclipse.m4a"

cd ../..

# Album 2: The Beatles - Abbey Road (1969)
echo "Creating The Beatles - Abbey Road (1969)..."
mkdir -p "The Beatles/Abbey Road (1969)"
cd "The Beatles/Abbey Road (1969)"

create_track "1" "Come Together" "The Beatles" "Abbey Road" "1969" "Rock" "01 - Come Together.m4a"
create_track "2" "Something" "The Beatles" "Abbey Road" "1969" "Rock" "02 - Something.m4a"
create_track "3" "Maxwell's Silver Hammer" "The Beatles" "Abbey Road" "1969" "Rock" "03 - Maxwell's Silver Hammer.m4a"
create_track "4" "Oh! Darling" "The Beatles" "Abbey Road" "1969" "Rock" "04 - Oh! Darling.m4a"
create_track "5" "Octopus's Garden" "The Beatles" "Abbey Road" "1969" "Rock" "05 - Octopus's Garden.m4a"
create_track "6" "I Want You (She's So Heavy)" "The Beatles" "Abbey Road" "1969" "Rock" "06 - I Want You (She's So Heavy).m4a"
create_track "7" "Here Comes the Sun" "The Beatles" "Abbey Road" "1969" "Rock" "07 - Here Comes the Sun.m4a"
create_track "8" "Because" "The Beatles" "Abbey Road" "1969" "Rock" "08 - Because.m4a"
create_track "9" "You Never Give Me Your Money" "The Beatles" "Abbey Road" "1969" "Rock" "09 - You Never Give Me Your Money.m4a"
create_track "10" "Sun King" "The Beatles" "Abbey Road" "1969" "Rock" "10 - Sun King.m4a"
create_track "11" "Mean Mr. Mustard" "The Beatles" "Abbey Road" "1969" "Rock" "11 - Mean Mr. Mustard.m4a"
create_track "12" "Polythene Pam" "The Beatles" "Abbey Road" "1969" "Rock" "12 - Polythene Pam.m4a"
create_track "13" "She Came in Through the Bathroom Window" "The Beatles" "Abbey Road" "1969" "Rock" "13 - She Came in Through the Bathroom Window.m4a"
create_track "14" "Golden Slumbers" "The Beatles" "Abbey Road" "1969" "Rock" "14 - Golden Slumbers.m4a"
create_track "15" "Carry That Weight" "The Beatles" "Abbey Road" "1969" "Rock" "15 - Carry That Weight.m4a"
create_track "16" "The End" "The Beatles" "Abbey Road" "1969" "Rock" "16 - The End.m4a"
create_track "17" "Her Majesty" "The Beatles" "Abbey Road" "1969" "Rock" "17 - Her Majesty.m4a"

cd ../..
# Album 3: Michael Jackson - Thriller (1982)
echo "Creating Michael Jackson - Thriller (1982)..."
mkdir -p "Michael Jackson/Thriller (1982)"
cd "Michael Jackson/Thriller (1982)"

create_track "1" "Wanna Be Startin' Somethin'" "Michael Jackson" "Thriller" "1982" "Pop" "01 - Wanna Be Startin' Somethin'.m4a"
create_track "2" "Baby Be Mine" "Michael Jackson" "Thriller" "1982" "Pop" "02 - Baby Be Mine.m4a"
create_track "3" "The Girl Is Mine" "Michael Jackson" "Thriller" "1982" "Pop" "03 - The Girl Is Mine.m4a"
create_track "4" "Thriller" "Michael Jackson" "Thriller" "1982" "Pop" "04 - Thriller.m4a"
create_track "5" "Beat It" "Michael Jackson" "Thriller" "1982" "Pop" "05 - Beat It.m4a"
create_track "6" "Billie Jean" "Michael Jackson" "Thriller" "1982" "Pop" "06 - Billie Jean.m4a"
create_track "7" "Human Nature" "Michael Jackson" "Thriller" "1982" "Pop" "07 - Human Nature.m4a"
create_track "8" "P.Y.T. (Pretty Young Thing)" "Michael Jackson" "Thriller" "1982" "Pop" "08 - P.Y.T. (Pretty Young Thing).m4a"
create_track "9" "The Lady in My Life" "Michael Jackson" "Thriller" "1982" "Pop" "09 - The Lady in My Life.m4a"

cd ../..

# Album 4: Led Zeppelin - Led Zeppelin IV (1971)
echo "Creating Led Zeppelin - Led Zeppelin IV (1971)..."
mkdir -p "Led Zeppelin/Led Zeppelin IV (1971)"
cd "Led Zeppelin/Led Zeppelin IV (1971)"

create_track "1" "Black Dog" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "01 - Black Dog.m4a"
create_track "2" "Rock and Roll" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "02 - Rock and Roll.m4a"
create_track "3" "The Battle of Evermore" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "03 - The Battle of Evermore.m4a"
create_track "4" "Stairway to Heaven" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "04 - Stairway to Heaven.m4a"
create_track "5" "Misty Mountain Hop" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "05 - Misty Mountain Hop.m4a"
create_track "6" "Four Sticks" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "06 - Four Sticks.m4a"
create_track "7" "Going to California" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "07 - Going to California.m4a"
create_track "8" "When the Levee Breaks" "Led Zeppelin" "Led Zeppelin IV" "1971" "Hard Rock" "08 - When the Levee Breaks.m4a"

cd ../..
# Album 5: Nirvana - Nevermind (1991)
echo "Creating Nirvana - Nevermind (1991)..."
mkdir -p "Nirvana/Nevermind (1991)"
cd "Nirvana/Nevermind (1991)"

create_track "1" "Smells Like Teen Spirit" "Nirvana" "Nevermind" "1991" "Grunge" "01 - Smells Like Teen Spirit.m4a"
create_track "2" "In Bloom" "Nirvana" "Nevermind" "1991" "Grunge" "02 - In Bloom.m4a"
create_track "3" "Come as You Are" "Nirvana" "Nevermind" "1991" "Grunge" "03 - Come as You Are.m4a"
create_track "4" "Breed" "Nirvana" "Nevermind" "1991" "Grunge" "04 - Breed.m4a"
create_track "5" "Lithium" "Nirvana" "Nevermind" "1991" "Grunge" "05 - Lithium.m4a"
create_track "6" "Polly" "Nirvana" "Nevermind" "1991" "Grunge" "06 - Polly.m4a"
create_track "7" "Territorial Pissings" "Nirvana" "Nevermind" "1991" "Grunge" "07 - Territorial Pissings.m4a"
create_track "8" "Drain You" "Nirvana" "Nevermind" "1991" "Grunge" "08 - Drain You.m4a"
create_track "9" "Lounge Act" "Nirvana" "Nevermind" "1991" "Grunge" "09 - Lounge Act.m4a"
create_track "10" "Stay Away" "Nirvana" "Nevermind" "1991" "Grunge" "10 - Stay Away.m4a"
create_track "11" "On a Plain" "Nirvana" "Nevermind" "1991" "Grunge" "11 - On a Plain.m4a"
create_track "12" "Something in the Way" "Nirvana" "Nevermind" "1991" "Grunge" "12 - Something in the Way.m4a"

cd ../..

# Album 6: AC/DC - Back in Black (1980)
echo "Creating AC/DC - Back in Black (1980)..."
mkdir -p "AC DC/Back in Black (1980)"
cd "AC DC/Back in Black (1980)"

create_track "1" "Hells Bells" "AC/DC" "Back in Black" "1980" "Hard Rock" "01 - Hells Bells.m4a"
create_track "2" "Shoot to Thrill" "AC/DC" "Back in Black" "1980" "Hard Rock" "02 - Shoot to Thrill.m4a"
create_track "3" "What Do You Do for Money Honey" "AC/DC" "Back in Black" "1980" "Hard Rock" "03 - What Do You Do for Money Honey.m4a"
create_track "4" "Given the Dog a Bone" "AC/DC" "Back in Black" "1980" "Hard Rock" "04 - Given the Dog a Bone.m4a"
create_track "5" "Let Me Put My Love into You" "AC/DC" "Back in Black" "1980" "Hard Rock" "05 - Let Me Put My Love into You.m4a"
create_track "6" "Back in Black" "AC/DC" "Back in Black" "1980" "Hard Rock" "06 - Back in Black.m4a"
create_track "7" "You Shook Me All Night Long" "AC/DC" "Back in Black" "1980" "Hard Rock" "07 - You Shook Me All Night Long.m4a"
create_track "8" "Have a Drink on Me" "AC/DC" "Back in Black" "1980" "Hard Rock" "08 - Have a Drink on Me.m4a"
create_track "9" "Shake a Leg" "AC/DC" "Back in Black" "1980" "Hard Rock" "09 - Shake a Leg.m4a"
create_track "10" "Rock and Roll Ain't Noise Pollution" "AC/DC" "Back in Black" "1980" "Hard Rock" "10 - Rock and Roll Ain't Noise Pollution.m4a"

cd ../..
# Album 7: Eagles - Hotel California (1976)
echo "Creating Eagles - Hotel California (1976)..."
mkdir -p "Eagles/Hotel California (1976)"
cd "Eagles/Hotel California (1976)"

create_track "1" "Hotel California" "Eagles" "Hotel California" "1976" "Rock" "01 - Hotel California.m4a"
create_track "2" "New Kid in Town" "Eagles" "Hotel California" "1976" "Rock" "02 - New Kid in Town.m4a"
create_track "3" "Life in the Fast Lane" "Eagles" "Hotel California" "1976" "Rock" "03 - Life in the Fast Lane.m4a"
create_track "4" "Wasted Time" "Eagles" "Hotel California" "1976" "Rock" "04 - Wasted Time.m4a"
create_track "5" "Wasted Time (Reprise)" "Eagles" "Hotel California" "1976" "Rock" "05 - Wasted Time (Reprise).m4a"
create_track "6" "Victim of Love" "Eagles" "Hotel California" "1976" "Rock" "06 - Victim of Love.m4a"
create_track "7" "Pretty Maids All in a Row" "Eagles" "Hotel California" "1976" "Rock" "07 - Pretty Maids All in a Row.m4a"
create_track "8" "Try and Love Again" "Eagles" "Hotel California" "1976" "Rock" "08 - Try and Love Again.m4a"
create_track "9" "The Last Resort" "Eagles" "Hotel California" "1976" "Rock" "09 - The Last Resort.m4a"

cd ../..

# Album 8: Dr. Dre - The Chronic (1992)
echo "Creating Dr. Dre - The Chronic (1992)..."
mkdir -p "Dr. Dre/The Chronic (1992)"
cd "Dr. Dre/The Chronic (1992)"

create_track "1" "The Chronic (Intro)" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "01 - The Chronic (Intro).m4a"
create_track "2" "Fuck wit Dre Day (And Everybody's Celebratin')" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "02 - Fuck wit Dre Day (And Everybody's Celebratin').m4a"
create_track "3" "Let Me Ride" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "03 - Let Me Ride.m4a"
create_track "4" "The Day the Niggaz Took Over" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "04 - The Day the Niggaz Took Over.m4a"
create_track "5" "Nuthin' but a 'G' Thang" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "05 - Nuthin' but a 'G' Thang.m4a"
create_track "6" "Deeez Nuuuts" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "06 - Deeez Nuuuts.m4a"
create_track "7" "Lil' Ghetto Boy" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "07 - Lil' Ghetto Boy.m4a"
create_track "8" "A Nigga Witta Gun" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "08 - A Nigga Witta Gun.m4a"
create_track "9" "Rat-Tat-Tat-Tat" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "09 - Rat-Tat-Tat-Tat.m4a"
create_track "10" "The $20 Sack Pyramid" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "10 - The $20 Sack Pyramid.m4a"
create_track "11" "Lyrical Gangbang" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "11 - Lyrical Gangbang.m4a"
create_track "12" "High Powered" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "12 - High Powered.m4a"
create_track "13" "The Doctor's Office" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "13 - The Doctor's Office.m4a"
create_track "14" "Stranded on Death Row" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "14 - Stranded on Death Row.m4a"
create_track "15" "The Roach (The Chronic Outro)" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "15 - The Roach (The Chronic Outro).m4a"
create_track "16" "Bitches Ain't Shit" "Dr. Dre" "The Chronic" "1992" "Hip-Hop" "16 - Bitches Ain't Shit.m4a"

cd ../..
# Album 9: Nas - Illmatic (1994)
echo "Creating Nas - Illmatic (1994)..."
mkdir -p "Nas/Illmatic (1994)"
cd "Nas/Illmatic (1994)"

create_track "1" "The Genesis" "Nas" "Illmatic" "1994" "Hip-Hop" "01 - The Genesis.m4a"
create_track "2" "N.Y. State of Mind" "Nas" "Illmatic" "1994" "Hip-Hop" "02 - N.Y. State of Mind.m4a"
create_track "3" "Life's a Bitch" "Nas" "Illmatic" "1994" "Hip-Hop" "03 - Life's a Bitch.m4a"
create_track "4" "The World Is Yours" "Nas" "Illmatic" "1994" "Hip-Hop" "04 - The World Is Yours.m4a"
create_track "5" "Halftime" "Nas" "Illmatic" "1994" "Hip-Hop" "05 - Halftime.m4a"
create_track "6" "Memory Lane (Sittin' in da Park)" "Nas" "Illmatic" "1994" "Hip-Hop" "06 - Memory Lane (Sittin' in da Park).m4a"
create_track "7" "One Love" "Nas" "Illmatic" "1994" "Hip-Hop" "07 - One Love.m4a"
create_track "8" "One Time 4 Your Mind" "Nas" "Illmatic" "1994" "Hip-Hop" "08 - One Time 4 Your Mind.m4a"
create_track "9" "Represent" "Nas" "Illmatic" "1994" "Hip-Hop" "09 - Represent.m4a"
create_track "10" "It Ain't Hard to Tell" "Nas" "Illmatic" "1994" "Hip-Hop" "10 - It Ain't Hard to Tell.m4a"

cd ../..

# Album 10: The Notorious B.I.G. - Ready to Die (1994)
echo "Creating The Notorious B.I.G. - Ready to Die (1994)..."
mkdir -p "The Notorious B.I.G/Ready to Die (1994)"
cd "The Notorious B.I.G/Ready to Die (1994)"

create_track "1" "Intro" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "01 - Intro.m4a"
create_track "2" "Things Done Changed" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "02 - Things Done Changed.m4a"
create_track "3" "Gimme the Loot" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "03 - Gimme the Loot.m4a"
create_track "4" "Machine Gun Funk" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "04 - Machine Gun Funk.m4a"
create_track "5" "Warning" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "05 - Warning.m4a"
create_track "6" "Ready to Die" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "06 - Ready to Die.m4a"
create_track "7" "One More Chance" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "07 - One More Chance.m4a"
create_track "8" "Fuck Me (Interlude)" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "08 - Fuck Me (Interlude).m4a"
create_track "9" "The What" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "09 - The What.m4a"
create_track "10" "Juicy" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "10 - Juicy.m4a"
create_track "11" "Everyday Struggle" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "11 - Everyday Struggle.m4a"
create_track "12" "Me & My Bitch" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "12 - Me & My Bitch.m4a"
create_track "13" "Big Poppa" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "13 - Big Poppa.m4a"
create_track "14" "Respect" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "14 - Respect.m4a"
create_track "15" "Friend of Mine" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "15 - Friend of Mine.m4a"
create_track "16" "Unbelievable" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "16 - Unbelievable.m4a"
create_track "17" "Suicidal Thoughts" "The Notorious B.I.G." "Ready to Die" "1994" "Hip-Hop" "17 - Suicidal Thoughts.m4a"

cd ../..

echo ""
echo "🎵 Music library with metadata created successfully!"
echo "📊 Total albums: 10"
echo "🎤 Artists: Pink Floyd, The Beatles, Michael Jackson, Led Zeppelin, Nirvana, AC/DC, Eagles, Dr. Dre, Nas, The Notorious B.I.G."
echo "🎧 Total tracks: 119 with complete metadata"
echo ""
echo "📁 Directory structure:"
find . -type d -name "*(*)" | sort
echo ""
echo "🔍 Sample metadata check:"
echo "Run: ffprobe -v quiet -show_format -show_streams 'Pink Floyd/The Dark Side of the Moon (1973)/01 - Speak to Me.m4a'"
