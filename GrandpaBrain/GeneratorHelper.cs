/*
* Grandpa's Brain 
* Copyright (c) 2012 Weinian He
*    This program is free software: you can redistribute it and/or modify
*    it under the terms of the GNU Lesser General Public License as published by
*    the Free Software Foundation, either version 3 of the License, or
*    (at your option) any later version.
*
*    This program is distributed in the hope that it will be useful,
*    but WITHOUT ANY WARRANTY; without even the implied warranty of
*    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*    GNU Lesser General Public License for more details.
*
*    You should have received a copy of the GNU Lesser General Public License
*    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrandpaBrain
{
    public class GeneratorHelper
    {
	    private static Random opRand = new Random();
        private static Random numRand = new Random();
	    public static Operands GetRandomOp(){
            return (Operands)Enum.ToObject(typeof(Operands),opRand.Next(1,4));
	    }
        public static int GetRandomInt(int min, int max)
        {
            return numRand.Next(min, max);
        }
    }
}
