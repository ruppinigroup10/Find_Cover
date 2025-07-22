import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות
import 'local_storage_service.dart';
import 'add_known_location.dart';

class KnownLocationPage extends StatefulWidget {
  const KnownLocationPage({super.key});

  @override
  State<KnownLocationPage> createState() => _KnownLocationPageState();
}

class _KnownLocationPageState extends State<KnownLocationPage> {
  List<String> userLocations = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchUserLocations();
  }

  Future<void> _fetchUserLocations() async {
    final userData = await LocalStorageService.getUserData();
    final userId = userData['user_id'];
    // קריאה אמיתית לשרת לקבלת אזורים מוכרים
    try {
      final url = Uri.parse(
        'https://localhost:7203/api/User/GetKnownLocation?user_id=$userId',
      );
      final response = await http.get(url);
      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        final knownLocation = responseData['knownLocation'];
        if (knownLocation != null &&
            knownLocation is List &&
            knownLocation.isNotEmpty) {
          setState(() {
            userLocations = List<String>.from(
              knownLocation.map((loc) => loc['name'].toString()),
            );
            _isLoading = false;
          });
        } else {
          setState(() {
            userLocations = [];
            _isLoading = false;
          });
        }
      } else {
        setState(() {
          userLocations = [];
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        userLocations = [];
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: BasePage(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20.0),
          child: Column(
            children: [
              const SizedBox(height: 20),
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
                              (context) => const SettingsPage(title: 'הגדרות'),
                        ),
                      );
                    },
                  ),
                  Image.asset(
                    'assets/images/LOGO1.png',
                    width: 100,
                    height: 100,
                  ),
                  const SizedBox(width: 48),
                ],
              ),
              const SizedBox(height: 10),
              const Text(
                'אזורים מוכרים',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 20),
              Expanded(
                child:
                    _isLoading
                        ? const Center(child: CircularProgressIndicator())
                        : userLocations.isEmpty
                        ? const Center(child: Text('אין אזורים מוכרים'))
                        : SingleChildScrollView(
                          child: Column(
                            children: [
                              for (final location in userLocations)
                                Column(
                                  children: [
                                    _buildLocationButton(context, location),
                                    const SizedBox(height: 15),
                                  ],
                                ),
                              const SizedBox(height: 30),
                            ],
                          ),
                        ),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const AddKnownLocationPage(),
                    ),
                  );
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 40,
                    vertical: 15,
                  ),
                ),
                child: const Text(
                  'להוספת אזור מוכר',
                  style: TextStyle(fontSize: 16),
                ),
              ),
              const SizedBox(height: 310),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLocationButton(BuildContext context, String title) {
    return ElevatedButton(
      onPressed: () {
        // הוסף כאן את הפעולה הרצויה לכל כפתור
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
          Text(title, style: const TextStyle(fontSize: 16)),
          const Icon(Icons.arrow_forward_ios, size: 16),
        ],
      ),
    );
  }
}
