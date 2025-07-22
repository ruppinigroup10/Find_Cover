import React from "react";
import { Phone, Plus, Info } from "lucide-react";

const FindCoverHome = () => {
  return (
    <div className="flex flex-col items-center w-full h-full max-w-md mx-auto bg-white">
      {/* Mobile container with fixed dimensions */}
      <div className="flex flex-col items-center w-full h-full">
        {/* Logo and title section */}
        <div className="flex flex-col items-center pt-6 pb-4 w-full">
          {/* Logo */}
          <div className="mb-2">
            <img
              src="/logo.png"
              alt="Find Cover Logo"
              className="h-24 w-auto"
            />
          </div>

          {/* App Name */}
          {/* <h1 className="text-4xl font-bold text-gray-800 tracking-wide font-sans">
            FIND COVER
          </h1> */}

          {/* Hebrew subtitle */}
          <p className="text-sm text-gray-600 mt-1">
            יישומון חירום למרחב מוגן שיתופי
          </p>
        </div>

        {/* Menu Buttons - RTL text alignment for Hebrew */}
        <div className="w-full px-4 pb-4">
          <button className="w-full bg-gray-800 text-white py-3 rounded mb-3 text-right px-4 font-medium">
            ?שנתכונן לאזעקה הבאה
          </button>

          <button className="w-full bg-gray-800 text-white py-3 rounded mb-3 text-right px-4 font-medium">
            ?שנציל חיים היום
          </button>

          <button className="w-full bg-gray-800 text-white py-3 rounded mb-3 text-right px-4 font-medium">
            ?שנתכנן מסלול
          </button>
        </div>

        {/* Bottom navigation - fixed to bottom */}
        <div className="flex w-full border-t border-gray-300 mt-auto">
          <div className="flex-1 flex justify-center items-center py-4">
            <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
              <Plus size={18} className="text-gray-500" />
            </div>
          </div>

          <div className="flex-1 flex justify-center items-center py-4">
            <div className="w-12 h-12 bg-blue-200 rounded-full flex items-center justify-center -mt-4 border-4 border-white">
              <Phone size={22} className="text-gray-500" />
            </div>
          </div>

          <div className="flex-1 flex justify-center items-center py-4">
            <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
              <Info size={18} className="text-gray-500" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FindCoverHome;
