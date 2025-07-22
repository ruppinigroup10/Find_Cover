import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות
import 'Adding_shelter.dart'; // ייבוא עמוד הוספת מרחב מוגן
import 'update_My_Shelter.dart'; // ייבוא עמוד עדכון מקלט

final RouteObserver<PageRoute> routeObserver = RouteObserver<PageRoute>();

class MySheltersPage extends StatefulWidget {
  const MySheltersPage({super.key});

  @override
  State<MySheltersPage> createState() => _MySheltersPageState();
}

class _MySheltersPageState extends State<MySheltersPage> with RouteAware {
  List<Map<String, dynamic>> shelters = [];
  bool isLoading = true;
  String? shelterName;

  @override
  void initState() {
    super.initState();
    Future.delayed(
      const Duration(milliseconds: 500),
      fetchSheltersFromServer,
    ); // שליפה מהשרת לאחר חצי שניה
    _printUserInfo();
    _fetchKnownLocationsFromServer(); // קריאה ל-GetMyKnownLocations בכל כניסה
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    routeObserver.subscribe(this, ModalRoute.of(context)! as PageRoute);
  }

  @override
  void dispose() {
    routeObserver.unsubscribe(this);
    super.dispose();
  }

  @override
  void didPopNext() {
    // חוזרים לעמוד הזה אחרי הוספה/עריכה/מחיקה => רענון מהשרת
    fetchSheltersFromServer();
  }

  Future<void> _printUserInfo() async {
    // פונקציה ריקה - כל ההדפסות והמשתנים הוסרו
  }

  Future<void> _fetchKnownLocationsFromServer() async {
    final prefs = await SharedPreferences.getInstance();
    final userId = prefs.getString('user_id') ?? '';
    if (userId.isEmpty) return;
    try {
      final response = await http.get(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/getMyShelter?provider_id=$userId',
        ),
      );
    } catch (e) {
      // הדפסה הוסרה
    }
  }

  Future<void> fetchSheltersFromServer() async {
    setState(() {
      isLoading = true;
    });
    final prefs = await SharedPreferences.getInstance();
    final providerId =
        prefs.getString('UserId') ?? prefs.getString('user_id') ?? '';
    if (providerId.isEmpty) {
      setState(() {
        shelters = [];
        isLoading = false;
      });
      return;
    }
    try {
      final response = await http.get(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getMyShelter?provider_id=$providerId',
        ),
      );
      // print('תשובת השרת (getMyShelter): ���[31m${response.body}���[0m'); // הוסתרה הדפסת גוף התשובה
      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final List<dynamic> data = decoded['shelters'] ?? [];
        // הדפסה של שמות המרחבים המוגנים בלבד
        for (final shelter in data) {
          print(shelter['name']);
        }
        setState(() {
          shelters =
              data
                  .where((e) => e['name'] != null && e['shelter_id'] != null)
                  .map(
                    (e) => {
                      'name': e['name'].toString(),
                      'shelter_id': e['shelter_id'].toString(),
                    },
                  )
                  .toList();
          isLoading = false;
        });
      } else {
        setState(() {
          shelters = [];
          isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        shelters = [];
        isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Scaffold(
          backgroundColor: const Color(0xFFF8F5FB),
          body: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20.0),
            child: Column(
              children: [
                const SizedBox(height: 30),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      icon: const Icon(Icons.arrow_back_ios),
                      onPressed: () {
                        Navigator.pushReplacement(
                          context,
                          MaterialPageRoute(
                            builder:
                                (context) =>
                                    const SettingsPage(title: 'הגדרות'),
                          ),
                        );
                      },
                    ),
                    Image.asset(
                      'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(width: 48), // ריווח כדי לאזן את הלוגו
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'המרחבים המוגנים ששיתפתי',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                Expanded(
                  child:
                      isLoading
                          ? const Center(child: CircularProgressIndicator())
                          : (shelters.isNotEmpty
                              ? ListView.builder(
                                itemCount:
                                    shelters.length +
                                    1, // כפתור ההוספה הוא פריט אחרון
                                padding: const EdgeInsets.only(bottom: 30),
                                itemBuilder: (context, index) {
                                  if (index < shelters.length) {
                                    final shelter = shelters[index];
                                    return Padding(
                                      padding: const EdgeInsets.symmetric(
                                        vertical: 8.0,
                                      ),
                                      child: _buildShelterButton(
                                        context,
                                        shelter['name'] ?? '',
                                        shelter['shelter_id'] ?? '',
                                      ),
                                    );
                                  } else {
                                    // כפתור ההוספה
                                    return Column(
                                      children: [
                                        const SizedBox(height: 30),
                                        Center(
                                          child: ElevatedButton(
                                            onPressed: () async {
                                              final result = await Navigator.push(
                                                context,
                                                MaterialPageRoute(
                                                  builder:
                                                      (context) =>
                                                          const AddingShelterPage(),
                                                ),
                                              );
                                              if (result == true) {
                                                fetchSheltersFromServer(); // רענון מהשרת אחרי הוספה
                                              }
                                            },
                                            style: ElevatedButton.styleFrom(
                                              backgroundColor:
                                                  const Color.fromARGB(
                                                    255,
                                                    29,
                                                    46,
                                                    89,
                                                  ),
                                              foregroundColor: Colors.white,
                                              padding:
                                                  const EdgeInsets.symmetric(
                                                    horizontal: 40,
                                                    vertical: 15,
                                                  ),
                                              shape: RoundedRectangleBorder(
                                                borderRadius:
                                                    BorderRadius.circular(30),
                                              ),
                                            ),
                                            child: const Text(
                                              'להוספת מרחב מוגן',
                                              style: TextStyle(fontSize: 16),
                                            ),
                                          ),
                                        ),
                                      ],
                                    );
                                  }
                                },
                              )
                              : Column(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  const Text(
                                    'עדיין לא הוספת מרחב מוגן',
                                    style: TextStyle(
                                      fontSize: 18,
                                      color: Colors.black54,
                                    ),
                                  ),
                                  const SizedBox(height: 30),
                                  Center(
                                    child: ElevatedButton(
                                      onPressed: () async {
                                        final result = await Navigator.push(
                                          context,
                                          MaterialPageRoute(
                                            builder:
                                                (context) =>
                                                    const AddingShelterPage(),
                                          ),
                                        );
                                        if (result == true) {
                                          fetchSheltersFromServer(); // רענון מהשרת אחרי הוספה
                                        }
                                      },
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: const Color.fromARGB(
                                          255,
                                          29,
                                          46,
                                          89,
                                        ),
                                        foregroundColor: Colors.white,
                                        padding: const EdgeInsets.symmetric(
                                          horizontal: 40,
                                          vertical: 15,
                                        ),
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(
                                            30,
                                          ),
                                        ),
                                      ),
                                      child: const Text(
                                        'להוספת מרחב מוגן',
                                        style: TextStyle(fontSize: 16),
                                      ),
                                    ),
                                  ),
                                ],
                              )),
                ),
              ],
            ),
          ),
          // הסר bottomNavigationBar
        ),
      ),
    );
  }

  Widget _buildShelterButton(
    BuildContext context,
    String title,
    String shelterId,
  ) {
    return ElevatedButton(
      onPressed: () async {
        await Future.delayed(const Duration(milliseconds: 500));
        Map<String, dynamic>? shelter;
        try {
          final response = await http.get(
            Uri.parse(
              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getShelter?shelter_id=$shelterId',
            ),
          );
          if (response.statusCode == 200) {
            final decoded = jsonDecode(response.body);
            shelter =
                decoded['shlter'] ??
                decoded['shelter'] ??
                decoded['Shelter'] ??
                decoded['ShelterData'];
            if (shelter != null) {
              print('--- כל הערכים שהתקבלו מהשרת עבור מקלט זה ---');
              shelter.forEach((key, value) {
                print('  $key: $value');
              });
              // שליחת הערכים לעמוד העריכה
              final result = await Navigator.push(
                context,
                MaterialPageRoute(
                  builder:
                      (context) => UpdateMyShelterPage(shelterId: shelterId),
                ),
              );
              if (result == true) {
                fetchSheltersFromServer(); // רענון מהשרת אחרי עריכה
              }
            }
          }
        } catch (e) {
          // טיפול בשגיאה
        }
      },
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFFB0C4DE),
        foregroundColor: Colors.black,
        padding: const EdgeInsets.symmetric(vertical: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(title, style: const TextStyle(fontSize: 16)), // הטקסט בצד שמאל
          const Icon(Icons.arrow_forward_ios, size: 16), // החץ בצד ימין
        ],
      ),
    );
  }
}
