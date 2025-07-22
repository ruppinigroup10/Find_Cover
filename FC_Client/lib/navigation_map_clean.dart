// // ignore_for_file: avoid_print

// import 'dart:io';
// import 'dart:async';
// import 'dart:convert';
// import 'package:flutter/material.dart';
// import 'package:google_maps_flutter/google_maps_flutter.dart';
// import 'package:geolocator/geolocator.dart';
// import 'package:http/http.dart' as http;
// import 'package:shared_preferences/shared_preferences.dart';
// import 'base_page_emergency.dart';
// import 'arrival_success_page.dart';

// /// גרסה נקייה של מסך הניווט
// ///
// /// מסך זה מקבל את המיקום הנוכחי של המשתמש ושולח בקשה לשרת
// /// לקבלת המרחב המוגן הקרוב ביותר.
// ///
// /// התהליך:
// /// 1. קבלת מיקום GPS של המשתמש
// /// 2. שליחת המיקום + מזהה משתמש לשרת בכתובת:
// ///    https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route
// /// 3. קבלת קואורדינטות המרחב המוגן מהשרת והצגתו כיעד במפה
// ///
// /// אם יש שגיאה בכל שלב - מוצגת הודעת שגיאה ברורה למשתמש.

// class NavigationMapPage extends StatefulWidget {
//   final Map<String, dynamic>? shelterDetails; // פרטי המרחב המוגן מהשרת

//   const NavigationMapPage({Key? key, this.shelterDetails}) : super(key: key);

//   @override
//   State<NavigationMapPage> createState() => _NavigationMapPageState();
// }

// class _NavigationMapPageState extends State<NavigationMapPage> {
//   // Use Completer for robust controller management
//   final Completer<GoogleMapController> _mapControllerCompleter =
//       Completer<GoogleMapController>();
//   LatLng? _currentPosition;
//   bool _loading = true;
//   String? _error;
//   LatLng? _destination; // יעד הניווט - null אם לא הוגדר
//   bool _showArrivedButton = false; // מוצג רק אם יש יעד תקין
//   String? _destinationError; // הודעת שגיאה ליעד
//   bool _loadingDestination = true; // טוען יעד מהשרת
//   bool _disposed = false; // Track disposal state

//   @override
//   void initState() {
//     super.initState();
//     print("NavigationMapPage: initState started");

//     // טעינת מיקום מ-GPS אמיתי
//     if (!Platform.isWindows) {
//       _determinePosition()
//           .then((_) {
//             // לאחר קבלת המיקום, שלח לשרת לקבלת יעד
//             if (_currentPosition != null) {
//               _getShelterRouteFromServer();
//             }
//           })
//           .catchError((error) {
//             print("NavigationMapPage: Error in _determinePosition: $error");
//             if (mounted) {
//               setState(() {
//                 _loading = false;
//                 _error = 'שגיאה בקבלת מיקום: $error';
//               });
//             }
//           });
//     } else {
//       // ב-Windows, נסיים את הטעינה ללא שגיאה
//       setState(() {
//         _loading = false;
//         _destinationError = 'מיקום לא זמין ב-Windows';
//       });
//     }
//   }

//   /// קריאה לשרת לקבלת מסלול למרחב מוגן
//   Future<void> _getShelterRouteFromServer() async {
//     if (_currentPosition == null) {
//       setState(() {
//         _destinationError = 'שגיאה: לא ניתן לקבל מיקום נוכחי';
//         _loadingDestination = false;
//       });
//       return;
//     }

//     try {
//       print("NavigationMapPage: Getting shelter route from server...");

//       // קריאת מזהה המשתמש מ-SharedPreferences
//       final prefs = await SharedPreferences.getInstance();
//       final userId =
//           prefs.getString('user_id') ?? prefs.getInt('user_id')?.toString();

//       print("NavigationMapPage: User ID from SharedPreferences: $userId");

//       if (userId == null) {
//         print("NavigationMapPage: No user ID found in SharedPreferences");
//         setState(() {
//           _destinationError = 'שגיאה: לא נמצא מזהה משתמש';
//           _loadingDestination = false;
//         });
//         return;
//       }

//       // הכנת הנתונים לשליחה לשרת
//       final requestBody = {
//         'userId': userId,
//         'latitude': _currentPosition!.latitude,
//         'longitude': _currentPosition!.longitude,
//       };

//       print("NavigationMapPage: Sending request with data: $requestBody");

//       // שליחת בקשה לשרת
//       final response = await http.post(
//         Uri.parse(
//           'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route',
//         ),
//         headers: {'Content-Type': 'application/json'},
//         body: jsonEncode(requestBody),
//       );

//       print(
//         "NavigationMapPage: Server response status: ${response.statusCode}",
//       );
//       print("NavigationMapPage: Server response body: ${response.body}");

//       if (response.statusCode == 200) {
//         final responseData = jsonDecode(response.body);

//         print("NavigationMapPage: Parsed response data: $responseData");

//         // הנחה שהשרת מחזיר: {"latitude": 32.xxxx, "longitude": 34.xxxx, "shelterName": "שם המרחב"}
//         final lat = responseData['latitude']?.toDouble();
//         final lng = responseData['longitude']?.toDouble();
//         final shelterName =
//             responseData['shelterName'] ?? responseData['name'] ?? 'מרחב מוגן';

//         if (lat != null && lng != null) {
//           setState(() {
//             _destination = LatLng(lat, lng);
//             _showArrivedButton = true;
//             _loadingDestination = false;
//             _destinationError = null;
//           });

//           print(
//             "NavigationMapPage: Destination set to: $lat, $lng ($shelterName)",
//           );

//           // שמירת הנתונים לשימוש עתידי
//           await prefs.setString(
//             'shelter_route_data',
//             jsonEncode({
//               'shelterDetails': {
//                 'latitude': lat,
//                 'longitude': lng,
//                 'name': shelterName,
//               },
//             }),
//           );
//         } else {
//           setState(() {
//             _destinationError = 'שגיאה: השרת החזיר קואורדינטות לא תקינות';
//             _loadingDestination = false;
//           });
//         }
//       } else {
//         setState(() {
//           _destinationError = 'שגיאה בקריאה לשרת: ${response.statusCode}';
//           _loadingDestination = false;
//         });
//       }
//     } catch (e) {
//       print("NavigationMapPage: Exception in _getShelterRouteFromServer: $e");
//       setState(() {
//         _destinationError = 'שגיאה בחיבור לשרת: ${e.toString()}';
//         _loadingDestination = false;
//       });
//     }
//   }

//   Future<void> _determinePosition() async {
//     print("NavigationMapPage: _determinePosition called");

//     try {
//       bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
//       if (!serviceEnabled) {
//         setState(() {
//           _loading = false;
//           _error = 'שירותי מיקום כבויים. יש להפעיל אותם בהגדרות.';
//         });
//         return;
//       }

//       LocationPermission permission = await Geolocator.checkPermission();
//       if (permission == LocationPermission.denied) {
//         permission = await Geolocator.requestPermission();
//         if (permission == LocationPermission.denied) {
//           setState(() {
//             _loading = false;
//             _error = 'הרשאות מיקום נדחו';
//           });
//           return;
//         }
//       }

//       if (permission == LocationPermission.deniedForever) {
//         setState(() {
//           _loading = false;
//           _error = 'הרשאות מיקום נדחו לצמיתות. יש לאפשר ידנית בהגדרות.';
//         });
//         return;
//       }

//       Position position = await Geolocator.getCurrentPosition(
//         locationSettings: const LocationSettings(
//           accuracy: LocationAccuracy.high,
//         ),
//       );

//       setState(() {
//         _currentPosition = LatLng(position.latitude, position.longitude);
//         _loading = false;
//         _error = null;
//       });

//       print(
//         "NavigationMapPage: Position determined: ${position.latitude}, ${position.longitude}",
//       );
//     } catch (e) {
//       print("NavigationMapPage: Exception in _determinePosition: $e");
//       setState(() {
//         _loading = false;
//         _error = 'שגיאה בקבלת מיקום: ${e.toString()}';
//       });
//     }
//   }

//   Set<Marker> _buildMarkers() {
//     Set<Marker> markers = {};

//     // מרקר למיקום הנוכחי
//     if (_currentPosition != null) {
//       markers.add(
//         Marker(
//           markerId: const MarkerId('current_location'),
//           position: _currentPosition!,
//           infoWindow: const InfoWindow(title: 'המיקום שלך'),
//         ),
//       );
//     }

//     // מרקר ליעד - רק אם קיים יעד תקין
//     if (_destination != null) {
//       markers.add(
//         Marker(
//           markerId: const MarkerId('destination'),
//           position: _destination!,
//           infoWindow: const InfoWindow(title: 'יעד'),
//           icon: BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueRed),
//         ),
//       );
//     }

//     return markers;
//   }

//   Set<Polyline> _buildPolylines() {
//     if (_currentPosition == null || _destination == null) return {};

//     return {
//       Polyline(
//         polylineId: const PolylineId('route'),
//         points: [_currentPosition!, _destination!],
//         color: Colors.blue,
//         width: 5,
//       ),
//     };
//   }

//   Widget _buildGoogleMap() {
//     try {
//       print("NavigationMapPage: Building GoogleMap widget");
//       return GoogleMap(
//         initialCameraPosition: CameraPosition(
//           target:
//               _currentPosition ??
//               _destination ??
//               const LatLng(32.0853, 34.7818), // תל אביב כברירת מחדל
//           zoom: 16,
//         ),
//         myLocationEnabled: true,
//         myLocationButtonEnabled: false,
//         markers: _buildMarkers(),
//         polylines: _buildPolylines(),
//         onMapCreated: (controller) {
//           print("NavigationMapPage: ===== MAP CREATED SUCCESSFULLY =====");
//           print("NavigationMapPage: GoogleMap created successfully");
//           if (!mounted) {
//             print(
//               "NavigationMapPage: Widget not mounted during onMapCreated, skipping",
//             );
//             return;
//           }

//           if (!_mapControllerCompleter.isCompleted) {
//             _mapControllerCompleter.complete(controller);
//             print("NavigationMapPage: Map controller assigned successfully");

//             // Use post-frame callback for better timing
//             WidgetsBinding.instance.addPostFrameCallback((_) {
//               if (mounted) {
//                 _animateToCurrentPosition();
//               } else {
//                 print(
//                   "NavigationMapPage: ❌ Widget is not mounted, skipping camera animation",
//                 );
//               }
//             });
//           }
//         },
//       );
//     } catch (e) {
//       print("NavigationMapPage: Exception in _buildGoogleMap: $e");
//       return Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             Icon(Icons.error, color: Colors.red, size: 40),
//             const SizedBox(height: 12),
//             Text(
//               'שגיאה בטעינת המפה: ${e.toString()}',
//               style: const TextStyle(color: Colors.red, fontSize: 16),
//               textAlign: TextAlign.center,
//             ),
//             const SizedBox(height: 16),
//             ElevatedButton(
//               onPressed: () {
//                 setState(() {
//                   _loading = true;
//                   _error = null;
//                 });
//                 _determinePosition();
//               },
//               child: const Text('נסה שוב'),
//             ),
//           ],
//         ),
//       );
//     }
//   }

//   Widget _buildMapContent() {
//     // בדיקה אם יש שגיאה בקואורדינטות היעד
//     if (_destinationError != null) {
//       return Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             Icon(Icons.error_outline, size: 64, color: Colors.red[400]),
//             const SizedBox(height: 16),
//             Text(
//               'שגיאה בטעינת המרחב המוגן',
//               style: TextStyle(
//                 fontSize: 18,
//                 fontWeight: FontWeight.bold,
//                 color: Colors.red[700],
//               ),
//             ),
//             const SizedBox(height: 8),
//             Padding(
//               padding: const EdgeInsets.symmetric(horizontal: 16),
//               child: Text(
//                 _destinationError!,
//                 textAlign: TextAlign.center,
//                 style: TextStyle(fontSize: 14, color: Colors.red[600]),
//               ),
//             ),
//             const SizedBox(height: 16),
//             Row(
//               mainAxisAlignment: MainAxisAlignment.center,
//               children: [
//                 ElevatedButton(
//                   onPressed: () {
//                     Navigator.of(context).pop();
//                   },
//                   style: ElevatedButton.styleFrom(
//                     backgroundColor: Colors.red[600],
//                     foregroundColor: Colors.white,
//                   ),
//                   child: const Text('חזור'),
//                 ),
//                 const SizedBox(width: 16),
//                 ElevatedButton(
//                   onPressed: () async {
//                     setState(() {
//                       _loading = true;
//                       _loadingDestination = true;
//                       _destinationError = null;
//                     });
//                     await _determinePosition();
//                     if (_currentPosition != null) {
//                       await _getShelterRouteFromServer();
//                     }
//                   },
//                   style: ElevatedButton.styleFrom(
//                     backgroundColor: Colors.blue[600],
//                     foregroundColor: Colors.white,
//                   ),
//                   child: const Text('נסה שוב'),
//                 ),
//               ],
//             ),
//           ],
//         ),
//       );
//     }

//     // אם עדיין טוען מיקום או יעד
//     if (_loading || _loadingDestination) {
//       return const Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             CircularProgressIndicator(),
//             SizedBox(height: 16),
//             Text(
//               'מחפש מרחב מוגן הקרוב אליך...',
//               style: TextStyle(fontSize: 16),
//             ),
//             SizedBox(height: 8),
//             Text(
//               'אנא המתן...',
//               style: TextStyle(fontSize: 12, color: Colors.grey),
//             ),
//           ],
//         ),
//       );
//     }

//     if (_error != null) {
//       return Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             Icon(Icons.error, color: Colors.red, size: 40),
//             const SizedBox(height: 12),
//             Text(
//               _error!,
//               style: const TextStyle(color: Colors.red, fontSize: 16),
//               textAlign: TextAlign.center,
//             ),
//             const SizedBox(height: 16),
//             ElevatedButton(
//               onPressed: () {
//                 setState(() {
//                   _loading = true;
//                   _error = null;
//                 });
//                 _determinePosition();
//               },
//               child: const Text('נסה שוב'),
//             ),
//           ],
//         ),
//       );
//     }

//     if (Platform.isWindows) {
//       return Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             Icon(Icons.map, size: 80, color: Colors.blue),
//             const SizedBox(height: 20),
//             Text(
//               'מסך ניווט',
//               style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
//             ),
//             const SizedBox(height: 10),
//             Text(
//               'מפות Google אינן נתמכות ב-Windows',
//               style: TextStyle(fontSize: 16, color: Colors.grey[600]),
//             ),
//             if (_currentPosition != null) ...[
//               const SizedBox(height: 20),
//               Container(
//                 padding: const EdgeInsets.all(16),
//                 decoration: BoxDecoration(
//                   color: Colors.blue[50],
//                   borderRadius: BorderRadius.circular(8),
//                 ),
//                 child: Column(
//                   children: [
//                     Text(
//                       'המיקום הנוכחי שלך:',
//                       style: TextStyle(
//                         fontWeight: FontWeight.bold,
//                         fontSize: 16,
//                       ),
//                     ),
//                     const SizedBox(height: 8),
//                     Text(
//                       'רוחב: ${_currentPosition!.latitude.toStringAsFixed(4)}',
//                       style: TextStyle(fontSize: 14),
//                     ),
//                     Text(
//                       'אורך: ${_currentPosition!.longitude.toStringAsFixed(4)}',
//                       style: TextStyle(fontSize: 14),
//                     ),
//                   ],
//                 ),
//               ),
//             ],
//           ],
//         ),
//       );
//     }

//     // עבור Android/iOS - הצגת Google Maps
//     return _buildGoogleMap();
//   }

//   /// Animates camera to current position after ensuring PlatformView is ready
//   Future<void> _animateToCurrentPosition() async {
//     if (_disposed || !mounted || _currentPosition == null) {
//       print(
//         "NavigationMapPage: Cannot animate - disposed/not mounted/no position",
//       );
//       return;
//     }

//     try {
//       final controller = await _mapControllerCompleter.future;

//       if (_disposed || !mounted) {
//         print(
//           "NavigationMapPage: Widget disposed/not mounted after await, skipping animation",
//         );
//         return;
//       }

//       print("NavigationMapPage: 🎯 Animating camera to: $_currentPosition");
//       try {
//         await controller.animateCamera(
//           CameraUpdate.newCameraPosition(
//             CameraPosition(target: _currentPosition!, zoom: 19.5, tilt: 25),
//           ),
//         );
//         print("NavigationMapPage: ✅ Camera animation completed successfully");
//       } catch (animateError) {
//         print(
//           "NavigationMapPage: ⚠️ animateCamera failed: $animateError, trying moveCamera instead",
//         );
//         controller.moveCamera(
//           CameraUpdate.newCameraPosition(
//             CameraPosition(target: _currentPosition!, zoom: 19.5, tilt: 25),
//           ),
//         );
//         print("NavigationMapPage: ✅ Camera moved successfully (fallback)");
//       }
//     } catch (e) {
//       if (e.toString().contains('channel-error') ||
//           e.toString().contains('Unable to establish connection') ||
//           e.toString().contains('PlatformException')) {
//         print(
//           "NavigationMapPage: ⚠️ Known platform channel error (safely ignoring): $e",
//         );
//       } else {
//         print("NavigationMapPage: ❌ Failed to animate camera: $e");
//       }
//     }
//   }

//   /// Safe camera animation function that prevents communication channel errors
//   Future<void> _safeCameraAnimation(CameraUpdate cameraUpdate) async {
//     try {
//       // Check if widget is disposed or mounted first
//       if (_disposed || !mounted) {
//         print(
//           "NavigationMapPage: Widget is disposed/not mounted, skipping camera animation",
//         );
//         return;
//       }

//       // Use the Completer to ensure we have a valid controller
//       if (!_mapControllerCompleter.isCompleted) {
//         print(
//           "NavigationMapPage: Map controller not ready, skipping camera animation",
//         );
//         return;
//       }

//       final controller = await _mapControllerCompleter.future;

//       // Double-check disposed/mounted state after async operation
//       if (_disposed || !mounted) {
//         print(
//           "NavigationMapPage: Widget is disposed/not mounted after await, skipping camera animation",
//         );
//         return;
//       }

//       print("NavigationMapPage: Performing safe camera animation");
//       try {
//         await controller.animateCamera(cameraUpdate);
//         print("NavigationMapPage: Camera animation completed successfully");
//       } catch (animateError) {
//         print(
//           "NavigationMapPage: ⚠️ animateCamera failed: $animateError, trying moveCamera instead",
//         );
//         controller.moveCamera(cameraUpdate);
//         print("NavigationMapPage: Camera moved successfully (fallback)");
//       }
//     } catch (e) {
//       if (e.toString().contains('MissingPluginException') ||
//           e.toString().contains('channel') ||
//           e.toString().contains('PlatformException') ||
//           e.toString().contains('channel-error') ||
//           e.toString().contains('Unable to establish connection') ||
//           e.toString().contains('Controller was disposed') ||
//           e.toString().contains('GoogleMap was ready') ||
//           e.toString().contains('Controller or widget state changed')) {
//         print(
//           "NavigationMapPage: Known Google Maps channel error (safely ignoring): $e",
//         );
//       } else {
//         print("NavigationMapPage: Unknown camera animation error: $e");
//       }
//     }
//   }

//   @override
//   void dispose() {
//     _disposed = true;
//     print("NavigationMapPage: Disposing widget");
//     super.dispose();
//   }

//   @override
//   Widget build(BuildContext context) {
//     return BasePage(
//       child: Stack(
//         children: [
//           Column(
//             children: [
//               Container(
//                 padding: const EdgeInsets.all(12),
//                 child: Row(
//                   mainAxisAlignment: MainAxisAlignment.center,
//                   children: [
//                     Image.asset('assets/images/LOGO1.png', height: 70),
//                   ],
//                 ),
//               ),
//               Expanded(
//                 child: Padding(
//                   padding: const EdgeInsets.all(16.0),
//                   child: Column(
//                     children: [
//                       Expanded(
//                         child: Container(
//                           decoration: BoxDecoration(
//                             borderRadius: BorderRadius.circular(12),
//                             border: Border.all(color: Colors.grey[300]!),
//                           ),
//                           child: ClipRRect(
//                             borderRadius: BorderRadius.circular(12),
//                             child: Container(
//                               width: double.infinity,
//                               color: Colors.grey[200],
//                               child: _buildMapContent(),
//                             ),
//                           ),
//                         ),
//                       ),
//                       if (_showArrivedButton && _destinationError == null) ...[
//                         const SizedBox(height: 16),
//                         ElevatedButton(
//                           onPressed: () {
//                             Navigator.of(context).push(
//                               MaterialPageRoute(
//                                 builder:
//                                     (context) => const ArrivalSuccessPage(),
//                               ),
//                             );
//                           },
//                           style: ElevatedButton.styleFrom(
//                             backgroundColor: const Color.fromARGB(
//                               255,
//                               29,
//                               46,
//                               89,
//                             ),
//                             foregroundColor: Colors.white,
//                             shape: RoundedRectangleBorder(
//                               borderRadius: BorderRadius.circular(12),
//                             ),
//                             padding: const EdgeInsets.symmetric(
//                               horizontal: 32,
//                               vertical: 16,
//                             ),
//                           ),
//                           child: const Text(
//                             'הגעתי למקום',
//                             style: TextStyle(fontSize: 18),
//                           ),
//                         ),
//                         const SizedBox(height: 16), // רווח נוסף בתחתית
//                       ],
//                     ],
//                   ),
//                 ),
//               ),
//             ],
//           ),
//           // כפתור חזרה בפינה העליונה שמאלית
//           Positioned(
//             top: 40,
//             left: 16,
//             child: Container(
//               decoration: BoxDecoration(
//                 color: Colors.white.withOpacity(0.9),
//                 shape: BoxShape.circle,
//                 boxShadow: [
//                   BoxShadow(
//                     color: Colors.black.withOpacity(0.2),
//                     blurRadius: 6,
//                     offset: const Offset(0, 2),
//                   ),
//                 ],
//               ),
//               child: IconButton(
//                 onPressed: () => Navigator.of(context).pop(),
//                 icon: const Icon(Icons.arrow_back, color: Colors.black87),
//                 iconSize: 24,
//               ),
//             ),
//           ),
//           // כפתור מיקום נוכחי בפינה התחתונה שמאלית
//           if (!Platform.isWindows && _currentPosition != null)
//             Positioned(
//               bottom:
//                   _showArrivedButton && _destinationError == null
//                       ? 120
//                       : 40, // מתאים לגובה בהתאם לכפתור "הגעתי"
//               left: 16,
//               child: Container(
//                 decoration: BoxDecoration(
//                   color: Colors.blue,
//                   shape: BoxShape.circle,
//                   boxShadow: [
//                     BoxShadow(
//                       color: Colors.black.withOpacity(0.3),
//                       blurRadius: 8,
//                       offset: const Offset(0, 4),
//                     ),
//                   ],
//                 ),
//                 child: IconButton(
//                   onPressed: () {
//                     if (_currentPosition != null &&
//                         _mapControllerCompleter.isCompleted) {
//                       _safeCameraAnimation(
//                         CameraUpdate.newLatLngZoom(_currentPosition!, 16),
//                       );
//                     }
//                   },
//                   icon: const Icon(Icons.my_location, color: Colors.white),
//                   iconSize: 24,
//                 ),
//               ),
//             ),
//         ],
//       ),
//     );
//   }
// }
